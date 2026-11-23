using System.ComponentModel.DataAnnotations;

namespace VmtFabric.Api.Configuration;

/// <summary>Bloqueo de cuenta por intentos fallidos y límite de peticiones a /api/auth.</summary>
public class LoginSettings
{
    public const string Section = "Login";

    [Range(3, 20)]
    public int MaxIntentosFallidos { get; set; } = 5;

    [Range(1, 1440)]
    public int MinutosBloqueo { get; set; } = 15;

    /// <summary>Peticiones por minuto y por IP a los endpoints de autenticación.</summary>
    [Range(1, 1000)]
    public int PeticionesPorMinuto { get; set; } = 10;
}
