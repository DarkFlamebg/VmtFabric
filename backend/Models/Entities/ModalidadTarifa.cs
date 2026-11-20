namespace VmtFabric.Api.Models.Entities;

public class ModalidadTarifa
{
    public const byte Mensual = 1;
    public const byte PorHora = 2;

    public byte ModalidadTarifaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
