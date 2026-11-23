using System.Text.Json;
using VmtFabric.Api.Data;
using VmtFabric.Api.Security;

namespace VmtFabric.Api.Services.Auditoria;

public sealed class AuditoriaService(VmtFabricDbContext db, ICurrentUser currentUser) : IAuditoriaService
{
    public void Registrar(string accion, string entidad, int? entidadId, int? usuarioId, object? detalle = null)
    {
        // La IP ayuda a investigar intentos de acceso; no se guardan contraseñas ni tokens (S-12).
        var datos = new Dictionary<string, object?> { ["ip"] = currentUser.Ip };
        if (detalle is not null)
            datos["datos"] = detalle;

        db.Auditorias.Add(new Models.Entities.Auditoria
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = JsonSerializer.Serialize(datos)
        });
    }
}
