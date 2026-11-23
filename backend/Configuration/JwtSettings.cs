using System.ComponentModel.DataAnnotations;

namespace VmtFabric.Api.Configuration;

/// <summary>
/// Emisión de tokens. <see cref="Key"/> es secreta: en desarrollo llega por <c>.env</c> (JWT_KEY)
/// y en producción por la variable de entorno <c>Jwt__Key</c>. Nunca va en appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave HMAC-SHA256: mínimo 32 caracteres (256 bits).</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutos { get; set; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDias { get; set; } = 7;
}
