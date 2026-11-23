namespace VmtFabric.Api.Models.DTOs.Usuarios;

/// <summary>Parámetros de GET /api/usuarios (query string).</summary>
public sealed class UsuarioFiltro
{
    /// <summary>Texto contenido en el nombre o el correo.</summary>
    public string? Buscar { get; init; }
    public byte? RolId { get; init; }
    public bool? Activo { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 20;
}
