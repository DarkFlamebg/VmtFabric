using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VmtFabric.Api.Middleware;
using VmtFabric.Api.Models.DTOs.Auth;
using VmtFabric.Api.Security;
using VmtFabric.Api.Services.Auth;

namespace VmtFabric.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Inicia sesión con correo y contraseña.</summary>
    /// <response code="401">Correo o contraseña incorrectos.</response>
    /// <response code="423">Cuenta bloqueada temporalmente por intentos fallidos.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteAuth)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    /// <summary>Entrega un access token nuevo. El refresh token usado deja de servir (rotación).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteAuth)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken ct) =>
        auth.RefreshAsync(request.RefreshToken, ct);

    /// <summary>Cierra la sesión revocando el refresh token. Funciona aunque el access token haya vencido.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteAuth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Cambia la contraseña del usuario actual (obligatorio si tiene una temporal).</summary>
    /// <response code="422">La contraseña actual no es correcta.</response>
    [HttpPost("cambiar-password")]
    [PermitirConPasswordPendiente]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<TokenResponse> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct) =>
        auth.CambiarPasswordAsync(request, ct);

    /// <summary>Usuario de la sesión (nombre, correo y rol para el header).</summary>
    [HttpGet("me")]
    [PermitirConPasswordPendiente]
    [ProducesResponseType<UsuarioActualDto>(StatusCodes.Status200OK)]
    public Task<UsuarioActualDto> Me(CancellationToken ct) => auth.ObtenerActualAsync(ct);
}
