namespace VmtFabric.Api.Models.DTOs.Usuarios;

public sealed record UsuarioDto(
    int UsuarioId,
    string Nombre,
    string Correo,
    byte RolId,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword,
    bool Bloqueado,
    DateTime? UltimoAcceso,
    DateTime FechaCreacion);
