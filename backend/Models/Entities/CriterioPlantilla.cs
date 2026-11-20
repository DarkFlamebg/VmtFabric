namespace VmtFabric.Api.Models.Entities;

public class CriterioPlantilla
{
    public int CriterioPlantillaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
