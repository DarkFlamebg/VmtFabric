namespace VmtFabric.Api.Security;

/// <summary>Nombres cortos de claims (JWT estándar). El mapeo automático a ClaimTypes está desactivado.</summary>
public static class ClaimNames
{
    public const string UsuarioId = "sub";
    public const string Nombre = "name";
    public const string Correo = "email";
    public const string Rol = "role";
    public const string TokenId = "jti";

    /// <summary>Presente ("true") cuando el usuario tiene una contraseña temporal pendiente de cambio.</summary>
    public const string DebeCambiarPassword = "pwd_change";
}
