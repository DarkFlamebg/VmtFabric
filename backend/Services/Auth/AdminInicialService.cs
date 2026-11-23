using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Data;
using VmtFabric.Api.Security;

namespace VmtFabric.Api.Services.Auth;

/// <summary>
/// El script de la BD crea el usuario administrador con el marcador &lt;REEMPLAZAR_CON_HASH&gt;.
/// Al arrancar, si el marcador sigue ahí y hay una contraseña configurada, se reemplaza por su hash
/// BCrypt y se obliga a cambiarla en el primer ingreso. Si ya se reemplazó, no hace nada.
/// </summary>
public sealed partial class AdminInicialService(
    VmtFabricDbContext db,
    IPasswordHasher hasher,
    IOptions<AdminInicialSettings> options,
    ILogger<AdminInicialService> logger)
{
    public async Task AplicarAsync(CancellationToken ct)
    {
        var pendientes = await db.Usuarios
            .Where(u => u.PasswordHash == AdminInicialSettings.MarcadorHashPendiente)
            .ToListAsync(ct);
        if (pendientes.Count == 0)
            return;

        var password = options.Value.Password;
        if (string.IsNullOrWhiteSpace(password) || password.Length < Validators.ReglasComunes.MinPassword)
        {
            LogSinPassword(logger, pendientes.Count);
            return;
        }

        foreach (var usuario in pendientes)
        {
            usuario.PasswordHash = hasher.Hash(password);
            usuario.DebeCambiarPassword = true;
        }
        await db.SaveChangesAsync(ct);
        LogAplicado(logger, pendientes.Count);
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "{Count} usuario(s) con contraseña pendiente y sin AdminInicial:Password (mín. 8 caracteres): no podrán iniciar sesión")]
    private static partial void LogSinPassword(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Contraseña inicial aplicada a {Count} usuario(s); deberán cambiarla al entrar")]
    private static partial void LogAplicado(ILogger logger, int count);
}
