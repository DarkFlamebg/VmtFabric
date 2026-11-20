namespace VmtFabric.Api.Models.Entities;

public class Cliente
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ruc { get; set; }
    public string? Industria { get; set; }
    public string? ContactoNombre { get; set; }
    public string? ContactoCorreo { get; set; }
    public string? ContactoTelefono { get; set; }
    public DateOnly? InicioRelacion { get; set; }
    public bool Activo { get; set; } = true;
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
    public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}
