namespace VmtFabric.Api.Models.Entities;

public class Auditoria
{
    public long AuditoriaId { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;   // Crear, Editar, Eliminar, Login, Exportar
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string? Detalle { get; set; }                 // JSON
    public DateTime Fecha { get; set; }
}
