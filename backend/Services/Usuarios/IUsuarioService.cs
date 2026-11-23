using VmtFabric.Api.Models.DTOs.Comun;
using VmtFabric.Api.Models.DTOs.Usuarios;

namespace VmtFabric.Api.Services.Usuarios;

/// <summary>Administración de usuarios (solo Administrador). Sin correo: las contraseñas temporales las entrega el Administrador.</summary>
public interface IUsuarioService
{
    Task<PaginaResultado<UsuarioDto>> ListarAsync(UsuarioFiltro filtro, CancellationToken ct);
    Task<UsuarioDto> ObtenerAsync(int usuarioId, CancellationToken ct);
    Task<PasswordTemporalResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct);
    Task<UsuarioDto> ActualizarAsync(int usuarioId, ActualizarUsuarioRequest request, CancellationToken ct);
    Task<UsuarioDto> CambiarEstadoAsync(int usuarioId, bool activo, CancellationToken ct);

    /// <summary>Genera una contraseña temporal, desbloquea la cuenta y cierra sus sesiones.</summary>
    Task<PasswordTemporalResponse> RestablecerPasswordAsync(int usuarioId, CancellationToken ct);

    Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct);
}
