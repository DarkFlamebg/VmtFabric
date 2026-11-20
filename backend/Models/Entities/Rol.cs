namespace VmtFabric.Api.Models.Entities;

public class Rol
{
    public const byte Administrador = 1;
    public const byte Gestor = 2;

    public byte RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
