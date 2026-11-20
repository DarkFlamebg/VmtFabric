namespace VmtFabric.Api.Models.Entities;

public class Usuario
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public byte RolId { get; set; }
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; } = true;
    public DateTime? UltimoAcceso { get; set; }
    public byte IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public Rol Rol { get; set; } = null!;
    public Usuario? Creador { get; set; }
}
