using VmtFabric.Api.Models.Constants;

namespace VmtFabric.Api.Models.Entities;

public class Aviso
{
    public int AvisoId { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = TipoAviso.Sistema;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? EntidadTipo { get; set; }
    public int? EntidadId { get; set; }
    public bool Leido { get; set; }
    public DateTime? FechaLectura { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
