namespace VmtFabric.Api.Models.Entities;

public class Proyecto
{
    public int ProyectoId { get; set; }
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public Cliente Cliente { get; set; } = null!;
}
