namespace VmtFabric.Api.Configuration;

/// <summary>
/// Contraseña temporal para el administrador creado por el script de la BD (hash pendiente).
/// Solo se usa si la BD todavía tiene el marcador; el admin debe cambiarla al entrar.
/// Desarrollo: ADMIN_PASSWORD en <c>.env</c>. Producción: <c>AdminInicial__Password</c>, una sola vez.
/// </summary>
public class AdminInicialSettings
{
    public const string Section = "AdminInicial";
    public const string MarcadorHashPendiente = "<REEMPLAZAR_CON_HASH>";

    public string? Password { get; set; }
}
