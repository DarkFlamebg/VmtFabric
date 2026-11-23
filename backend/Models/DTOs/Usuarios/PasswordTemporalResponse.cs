namespace VmtFabric.Api.Models.DTOs.Usuarios;

/// <summary>
/// Contraseña temporal generada por el servidor. Se muestra una sola vez al Administrador,
/// que la entrega al usuario por un canal privado; el usuario debe cambiarla al entrar.
/// </summary>
public sealed record PasswordTemporalResponse(UsuarioDto Usuario, string PasswordTemporal);
