namespace VmtFabric.Api.Models.Entities;

public class Tecnologia
{
    public int TecnologiaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public ICollection<ColaboradorTecnologia> Colaboradores { get; set; } = new List<ColaboradorTecnologia>();
}
