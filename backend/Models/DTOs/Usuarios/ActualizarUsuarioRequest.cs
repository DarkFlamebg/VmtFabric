namespace VmtFabric.Api.Models.DTOs.Usuarios;

public sealed record ActualizarUsuarioRequest(string Nombre, string Correo, byte RolId);
