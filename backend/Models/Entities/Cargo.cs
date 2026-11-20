namespace VmtFabric.Api.Models.Entities;

public class Cargo
{
    public int CargoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
