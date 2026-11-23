namespace VmtFabric.Api.Models.DTOs.Auth;

/// <summary>
/// Par de tokens. El access token se envía como "Authorization: Bearer ..."; el refresh token
/// solo sirve para POST /api/auth/refresh y se invalida cada vez que se usa (rotación).
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpira,
    string RefreshToken,
    DateTime RefreshTokenExpira,
    UsuarioActualDto Usuario);
