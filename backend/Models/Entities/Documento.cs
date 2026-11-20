namespace VmtFabric.Api.Models.Entities;

public class Documento
{
    public int DocumentoId { get; set; }
    public int? ColaboradorId { get; set; }
    public int? ClienteId { get; set; }
    public byte TipoDocumentoId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaAlmacen { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public bool Eliminado { get; set; }
    public int SubidoPor { get; set; }
    public DateTime FechaCarga { get; set; }

    public Colaborador? Colaborador { get; set; }
    public Cliente? Cliente { get; set; }
    public TipoDocumento TipoDocumento { get; set; } = null!;
}
