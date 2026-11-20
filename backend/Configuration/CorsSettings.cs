using System.ComponentModel.DataAnnotations;

namespace VmtFabric.Api.Configuration;

/// <summary>Orígenes autorizados a llamar a la API (frontend Angular). Nunca usar "*".</summary>
public class CorsSettings
{
    public const string Section = "Cors";

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; set; } = [];
}
