namespace VmtFabric.Api.Models.Constants;

/// <summary>Valores de <c>Auditoria.Accion</c> (VARCHAR(20)).</summary>
public static class AccionAuditoria
{
    public const string Crear = "Crear";
    public const string Editar = "Editar";
    public const string Eliminar = "Eliminar";
    public const string Login = "Login";
    public const string LoginFallido = "LoginFallido";
    public const string Logout = "Logout";
    public const string CambioPassword = "CambioPassword";
    public const string ResetPassword = "ResetPassword";
    public const string TokenReutilizado = "TokenReutilizado";
    public const string Exportar = "Exportar";
}
