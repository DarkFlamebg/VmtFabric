namespace VmtFabric.Api.Models.DTOs.Usuarios;

/// <summary>La contraseña no se recibe: la genera el servidor y se entrega una sola vez al Administrador.</summary>
public sealed record CrearUsuarioRequest(string Nombre, string Correo, byte RolId);
