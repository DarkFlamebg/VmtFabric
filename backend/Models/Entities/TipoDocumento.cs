namespace VmtFabric.Api.Models.Entities;

public class TipoDocumento
{
    public const byte CV = 1;
    public const byte Cedula = 2;
    public const byte Contrato = 3;
    public const byte Otro = 4;

    public byte TipoDocumentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
