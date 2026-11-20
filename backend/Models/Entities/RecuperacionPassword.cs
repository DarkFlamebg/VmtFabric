namespace VmtFabric.Api.Models.Entities;

public class RecuperacionPassword
{
    public int RecuperacionId { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public bool Usado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
