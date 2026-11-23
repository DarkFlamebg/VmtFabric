using VmtFabric.Api.Models.DTOs.Auth;

namespace VmtFabric.Api.Services.Auth;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    /// <summary>Rota el refresh token: el usado queda revocado y se entrega uno nuevo.</summary>
    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct);

    Task LogoutAsync(string refreshToken, CancellationToken ct);

    /// <summary>Cambia la contraseña del usuario actual, cierra sus otras sesiones y devuelve tokens nuevos.</summary>
    Task<TokenResponse> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct);

    Task<UsuarioActualDto> ObtenerActualAsync(CancellationToken ct);
}
