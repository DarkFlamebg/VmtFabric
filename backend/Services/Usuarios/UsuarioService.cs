using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VmtFabric.Api.Data;
using VmtFabric.Api.Exceptions;
using VmtFabric.Api.Models.Constants;
using VmtFabric.Api.Models.DTOs.Comun;
using VmtFabric.Api.Models.DTOs.Usuarios;
using VmtFabric.Api.Models.Entities;
using VmtFabric.Api.Security;
using VmtFabric.Api.Services.Auditoria;
using VmtFabric.Api.Services.Auth;

namespace VmtFabric.Api.Services.Usuarios;

public sealed class UsuarioService(
    VmtFabricDbContext db,
    IPasswordHasher hasher,
    ICurrentUser currentUser,
    IAuditoriaService auditoria,
    TimeProvider time) : IUsuarioService
{
    private const string CorreoDuplicado = "Ya existe un usuario con ese correo.";

    public async Task<PaginaResultado<UsuarioDto>> ListarAsync(UsuarioFiltro filtro, CancellationToken ct)
    {
        var query = db.Usuarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var texto = filtro.Buscar.Trim();
            query = query.Where(u => u.Nombre.Contains(texto) || u.Correo.Contains(texto));
        }
        if (filtro.RolId is { } rolId)
            query = query.Where(u => u.RolId == rolId);
        if (filtro.Activo is { } activo)
            query = query.Where(u => u.Activo == activo);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Nombre).ThenBy(u => u.UsuarioId)
            .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(ADto(Ahora()))
            .ToListAsync(ct);

        return new PaginaResultado<UsuarioDto>(items, total, filtro.Pagina, filtro.TamanoPagina);
    }

    public async Task<UsuarioDto> ObtenerAsync(int usuarioId, CancellationToken ct) =>
        await db.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId)
            .Select(ADto(Ahora()))
            .SingleOrDefaultAsync(ct)
        ?? throw NoEncontrado();

    public async Task<PasswordTemporalResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        var correo = AuthService.NormalizarCorreo(request.Correo);
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
            throw new ConflictException(CorreoDuplicado);

        var password = PasswordGenerator.Generar();
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Correo = correo,
            RolId = request.RolId,
            PasswordHash = hasher.Hash(password),
            Activo = true,
            DebeCambiarPassword = true,
            CreadoPor = currentUser.UsuarioIdRequerido
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);   // AuditoriaInterceptor registra "Crear" (sin el hash)

        return new PasswordTemporalResponse(await ObtenerAsync(usuario.UsuarioId, ct), password);
    }

    public async Task<UsuarioDto> ActualizarAsync(int usuarioId, ActualizarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await BuscarAsync(usuarioId, ct);
        var correo = AuthService.NormalizarCorreo(request.Correo);

        if (correo != usuario.Correo && await db.Usuarios.AnyAsync(u => u.Correo == correo && u.UsuarioId != usuarioId, ct))
            throw new ConflictException(CorreoDuplicado);

        if (usuario.RolId == Rol.Administrador && request.RolId != Rol.Administrador)
        {
            if (usuarioId == currentUser.UsuarioIdRequerido)
                throw new BusinessRuleException("No puede quitarse a sí mismo el rol de Administrador.");
            await ExigirOtroAdministradorActivoAsync(usuarioId, ct);
        }

        var rolCambio = usuario.RolId != request.RolId;
        usuario.Nombre = request.Nombre.Trim();
        usuario.Correo = correo;
        usuario.RolId = request.RolId;

        // El rol va dentro del access token: al cambiarlo se cierran sus sesiones para que entre con el rol nuevo.
        if (rolCambio)
            await RevocarSesionesAsync(usuarioId, ct);

        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(usuarioId, ct);
    }

    public async Task<UsuarioDto> CambiarEstadoAsync(int usuarioId, bool activo, CancellationToken ct)
    {
        var usuario = await BuscarAsync(usuarioId, ct);
        if (usuario.Activo == activo)
            return await ObtenerAsync(usuarioId, ct);

        if (!activo)
        {
            if (usuarioId == currentUser.UsuarioIdRequerido)
                throw new BusinessRuleException("No puede desactivar su propio usuario.");
            if (usuario.RolId == Rol.Administrador)
                await ExigirOtroAdministradorActivoAsync(usuarioId, ct);
            await RevocarSesionesAsync(usuarioId, ct);
        }

        usuario.Activo = activo;
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(usuarioId, ct);
    }

    public async Task<PasswordTemporalResponse> RestablecerPasswordAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == currentUser.UsuarioIdRequerido)
            throw new BusinessRuleException("Para su propio usuario use \"Cambiar contraseña\".");

        var usuario = await BuscarAsync(usuarioId, ct);
        if (!usuario.Activo)
            throw new BusinessRuleException("El usuario está inactivo. Actívelo antes de restablecer su contraseña.");

        var password = PasswordGenerator.Generar();
        usuario.PasswordHash = hasher.Hash(password);
        usuario.DebeCambiarPassword = true;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await RevocarSesionesAsync(usuarioId, ct);

        auditoria.Registrar(AccionAuditoria.ResetPassword, nameof(Usuario), usuarioId, currentUser.UsuarioId);
        await db.SaveChangesAsync(ct);
        return new PasswordTemporalResponse(await ObtenerAsync(usuarioId, ct), password);
    }

    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct) =>
        await db.Roles.AsNoTracking()
            .OrderBy(r => r.RolId)
            .Select(r => new RolDto(r.RolId, r.Nombre, r.Descripcion))
            .ToListAsync(ct);

    private async Task<Usuario> BuscarAsync(int usuarioId, CancellationToken ct) =>
        await db.Usuarios.SingleOrDefaultAsync(u => u.UsuarioId == usuarioId, ct) ?? throw NoEncontrado();

    /// <summary>Siempre debe quedar al menos un Administrador activo.</summary>
    private async Task ExigirOtroAdministradorActivoAsync(int usuarioId, CancellationToken ct)
    {
        var hayOtro = await db.Usuarios.AnyAsync(
            u => u.UsuarioId != usuarioId && u.Activo && u.RolId == Rol.Administrador, ct);
        if (!hayOtro)
            throw new BusinessRuleException("Debe quedar al menos un Administrador activo.");
    }

    private Task<int> RevocarSesionesAsync(int usuarioId, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && !t.Revocado)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revocado, true), ct);

    private static Expression<Func<Usuario, UsuarioDto>> ADto(DateTime ahora) => u => new UsuarioDto(
        u.UsuarioId, u.Nombre, u.Correo, u.RolId, u.Rol.Nombre, u.Activo, u.DebeCambiarPassword,
        u.BloqueadoHasta != null && u.BloqueadoHasta > ahora, u.UltimoAcceso, u.FechaCreacion);

    private DateTime Ahora() => time.GetUtcNow().UtcDateTime;

    private static NotFoundException NoEncontrado() => new("El usuario no existe.");
}
