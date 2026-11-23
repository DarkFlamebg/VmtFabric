using Microsoft.Data.SqlClient;

namespace VmtFabric.Api.Extensions;

/// <summary>
/// Lee <c>backend/.env</c> (formato CLAVE=valor) y lo traduce a configuración:
/// DB_* → <c>ConnectionStrings:VmtFabric</c>, JWT_KEY → <c>Jwt:Key</c>, ADMIN_PASSWORD → <c>AdminInicial:Password</c>.
/// Solo en Development: en producción todo llega por variables de entorno del hosting.
/// El archivo está en .gitignore; la plantilla para el equipo es <c>.env.example</c>.
/// </summary>
public static class DotEnvExtensions
{
    private static readonly string[] Requeridas = ["DB_SERVER", "DB_NAME", "DB_USER", "DB_PASSWORD", "JWT_KEY"];

    public static IConfigurationManager AddDotEnv(this IConfigurationManager config, IHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, ".env");
        if (!env.IsDevelopment() || !File.Exists(path))
            return config;

        var vars = Leer(path);

        var faltan = Requeridas.Where(k => string.IsNullOrWhiteSpace(vars.GetValueOrDefault(k))).ToArray();
        if (faltan.Length > 0)
            // Solo se nombran las claves, nunca los valores.
            throw new InvalidOperationException($"El archivo .env no tiene: {string.Join(", ", faltan)}. Ver .env.example.");

        // El builder escapa los valores: una contraseña con ';' no puede alterar la cadena.
        var csb = new SqlConnectionStringBuilder
        {
            DataSource = vars["DB_SERVER"],
            InitialCatalog = vars["DB_NAME"],
            UserID = vars["DB_USER"],
            Password = vars["DB_PASSWORD"],
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = false,
            MultipleActiveResultSets = true,
            ConnectTimeout = 30
        };

        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:VmtFabric"] = csb.ConnectionString,
            ["Jwt:Key"] = vars["JWT_KEY"]
        };
        if (vars.TryGetValue("ADMIN_PASSWORD", out var admin) && !string.IsNullOrWhiteSpace(admin))
            valores["AdminInicial:Password"] = admin;

        config.AddInMemoryCollection(valores);
        return config;
    }

    private static Dictionary<string, string> Leer(string path) =>
        File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last()[1].Trim().Trim('"'), StringComparer.OrdinalIgnoreCase);
}
