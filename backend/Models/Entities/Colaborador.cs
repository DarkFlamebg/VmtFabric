namespace VmtFabric.Api.Models.Entities;

public class Colaborador
{
    public int ColaboradorId { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public string? Ubicacion { get; set; }
    public byte EstadoColaboradorId { get; set; }
    public int CargoId { get; set; }
    public byte SeniorityId { get; set; }
    public decimal? CostoMensualRef { get; set; }
    public bool Eliminado { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? ModificadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public EstadoColaborador Estado { get; set; } = null!;
    public Cargo Cargo { get; set; } = null!;
    public Seniority Seniority { get; set; } = null!;
    public ICollection<ColaboradorTecnologia> Tecnologias { get; set; } = new List<ColaboradorTecnologia>();
    public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}
