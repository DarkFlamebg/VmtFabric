using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Data;
using VmtFabric.Api.Exceptions;
using VmtFabric.Api.Models.Constants;
using VmtFabric.Api.Models.DTOs.Auth;
using VmtFabric.Api.Models.Entities;
using VmtFabric.Api.Security;
using VmtFabric.Api.Services.Auditoria;

namespace VmtFabric.Api.Services.Auth;

public sealed class AuthService(
    VmtFabricDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    ICurrentUser currentUser,
    IAuditoriaService auditoria,
    IOptions<LoginSettings> loginOptions,
    TimeProvider time) : IAuthService
{
    // Mismo mensaje para correo inexistente, usuario inactivo o contraseña errónea: no revela qué falló.
    private const string CredencialesInvalidas = "Correo o contraseña incorrectos.";
    private const string SesionInvalida = "La sesión expiró o no es válida. Inicie sesión de nuevo.";
    private const string EntidadUsuario = nameof(Usuario);

    private readonly LoginSettings _login = loginOptions.Value;

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var correo = NormalizarCorreo(request.Correo);
        var usuario = await db.Usuarios.Include(u => u.Rol).SingleOrDefaultAsync(u => u.Correo == correo, ct);
        var ahora = Ahora();

        if (usuario is null || !usuario.Activo)
        {
            hasher.VerificacionFicticia(request.Password);
            auditoria.Registrar(AccionAuditoria.LoginFallido, EntidadUsuario, usuario?.UsuarioId, usuario?.UsuarioId,
                new { motivo = usuario is null ? "correo no registrado" : "usuario inactivo" });
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException(CredencialesInvalidas);
        }

        if (usuario.BloqueadoHasta > ahora)
            throw new CuentaBloqueadaException(MensajeBloqueo(usuario.BloqueadoHasta.Value, ahora));

        if (!hasher.Verificar(request.Password, usuario.PasswordHash))
        {
            await RegistrarIntentoFallidoAsync(usuario, ahora, ct);
            if (usuario.BloqueadoHasta > ahora)
                throw new CuentaBloqueadaException(MensajeBloqueo(usuario.BloqueadoHasta.Value, ahora));
            throw new UnauthorizedException(CredencialesInvalidas);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = ahora;
        if (hasher.NecesitaRehash(usuario.PasswordHash))
            usuario.PasswordHash = hasher.Hash(request.Password);

        await EliminarTokensVencidosAsync(usuario.UsuarioId, ahora, ct);
        var respuesta = EmitirTokens(usuario);
        auditoria.Registrar(AccionAuditoria.Login, EntidadUsuario, usuario.UsuarioId, usuario.UsuarioId);
        await db.SaveChangesAsync(ct);
        return respuesta;
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var guardado = await db.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedException(SesionInvalida);

        var ahora = Ahora();

        // Revocación atómica: si dos peticiones usan el mismo token, solo una gana.
        var revocados = await db.RefreshTokens
            .Where(t => t.RefreshTokenId == guardado.RefreshTokenId && !t.Revocado)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revocado, true), ct);

        if (revocados == 0)
        {
            // Un token ya usado vuelve a aparecer: posible robo. Se cierran todas las sesiones del usuario.
            await RevocarTodosAsync(guardado.UsuarioId, ct);
            auditoria.Registrar(AccionAuditoria.TokenReutilizado, EntidadUsuario, guardado.UsuarioId, guardado.UsuarioId);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException(SesionInvalida);
        }

        var usuario = await db.Usuarios.Include(u => u.Rol)
            .SingleAsync(u => u.UsuarioId == guardado.UsuarioId, ct);

        if (guardado.Expira <= ahora || !usuario.Activo || usuario.BloqueadoHasta > ahora)
            throw new UnauthorizedException(SesionInvalida);

        var respuesta = EmitirTokens(usuario);
        await db.SaveChangesAsync(ct);
        return respuesta;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var guardado = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        // Logout es idempotente: un token desconocido o ya revocado no es un error.
        if (guardado is null || guardado.Revocado)
            return;

        guardado.Revocado = true;
        auditoria.Registrar(AccionAuditoria.Logout, EntidadUsuario, guardado.UsuarioId, guardado.UsuarioId);
        await db.SaveChangesAsync(ct);
    }

    public async Task<TokenResponse> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct)
    {
        var usuario = await ObtenerUsuarioActualAsync(ct);

        if (!hasher.Verificar(request.PasswordActual, usuario.PasswordHash))
            throw new BusinessRuleException("La contraseña actual no es correcta.");

        usuario.PasswordHash = hasher.Hash(request.PasswordNueva);
        usuario.DebeCambiarPassword = false;

        // Las demás sesiones (otros navegadores) quedan cerradas.
        await RevocarTodosAsync(usuario.UsuarioId, ct);
        var respuesta = EmitirTokens(usuario);
        auditoria.Registrar(AccionAuditoria.CambioPassword, EntidadUsuario, usuario.UsuarioId, usuario.UsuarioId);
        await db.SaveChangesAsync(ct);
        return respuesta;
    }

    public async Task<UsuarioActualDto> ObtenerActualAsync(CancellationToken ct)
    {
        var id = currentUser.UsuarioIdRequerido;
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioId == id && u.Activo)
            .Select(u => new UsuarioActualDto(u.UsuarioId, u.Nombre, u.Correo, u.Rol.Nombre, u.DebeCambiarPassword))
            .SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedException(SesionInvalida);
    }

    private async Task<Usuario> ObtenerUsuarioActualAsync(CancellationToken ct)
    {
        var id = currentUser.UsuarioIdRequerido;
        return await db.Usuarios.Include(u => u.Rol).SingleOrDefaultAsync(u => u.UsuarioId == id && u.Activo, ct)
            ?? throw new UnauthorizedException(SesionInvalida);
    }

    private TokenResponse EmitirTokens(Usuario usuario)
    {
        var (access, accessExpira) = tokens.CrearAccessToken(usuario);
        var (refresh, refreshHash, refreshExpira) = tokens.CrearRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UsuarioId = usuario.UsuarioId, TokenHash = refreshHash, Expira = refreshExpira });

        return new TokenResponse(access, accessExpira, refresh, refreshExpira,
            new UsuarioActualDto(usuario.UsuarioId, usuario.Nombre, usuario.Correo, usuario.Rol.Nombre, usuario.DebeCambiarPassword));
    }

    private async Task RegistrarIntentoFallidoAsync(Usuario usuario, DateTime ahora, CancellationToken ct)
    {
        usuario.IntentosFallidos++;
        if (usuario.IntentosFallidos >= _login.MaxIntentosFallidos)
        {
            usuario.BloqueadoHasta = ahora.AddMinutes(_login.MinutosBloqueo);
            usuario.IntentosFallidos = 0;
        }

        auditoria.Registrar(AccionAuditoria.LoginFallido, EntidadUsuario, usuario.UsuarioId, usuario.UsuarioId,
            new { motivo = "contraseña incorrecta", bloqueado = usuario.BloqueadoHasta > ahora });
        await db.SaveChangesAsync(ct);
    }

    private Task<int> RevocarTodosAsync(int usuarioId, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && !t.Revocado)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revocado, true), ct);

    // Limpieza: la tabla no crece con tokens vencidos. Los revocados se conservan hasta vencer
    // para poder detectar su reutilización en RefreshAsync.
    private Task<int> EliminarTokensVencidosAsync(int usuarioId, DateTime ahora, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && t.Expira <= ahora)
            .ExecuteDeleteAsync(ct);

    private static string MensajeBloqueo(DateTime hasta, DateTime ahora)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalMinutes));
        return $"Cuenta bloqueada por intentos fallidos. Intente de nuevo en {minutos} minuto(s) o pida al Administrador que restablezca su contraseña.";
    }

    private DateTime Ahora() => time.GetUtcNow().UtcDateTime;

    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();
}
