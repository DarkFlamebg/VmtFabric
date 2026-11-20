namespace VmtFabric.Api.Models.Entities;

public class Seniority
{
    public byte SeniorityId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Orden { get; set; }
}
