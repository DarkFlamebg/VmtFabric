namespace VmtFabric.Api.Middleware;

/// <summary>
/// Marca los endpoints que un usuario con contraseña temporal puede usar antes de cambiarla
/// (ver perfil, cambiar contraseña, cerrar sesión). El resto responde 403 DEBE_CAMBIAR_PASSWORD.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class PermitirConPasswordPendienteAttribute : Attribute;
