namespace VmtFabric.Api.Models.Entities;

public class AsignacionTecnologia
{
    public int AsignacionId { get; set; }
    public int TecnologiaId { get; set; }

    public Asignacion Asignacion { get; set; } = null!;
    public Tecnologia Tecnologia { get; set; } = null!;
}
