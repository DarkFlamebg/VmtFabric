namespace VmtFabric.Api.Security;

/// <summary>Políticas de autorización (S-02). Los nombres de rol coinciden con la tabla Rol.</summary>
public static class Politicas
{
    /// <summary>Solo Administrador: módulo de Administración (usuarios, catálogos).</summary>
    public const string Administrador = "Administrador";

    /// <summary>Administrador o Gestor: módulo Talento.</summary>
    public const string Talento = "Talento";

    /// <summary>Límite de peticiones a los endpoints de autenticación.</summary>
    public const string LimiteAuth = "auth";

    public const string RolAdministrador = "Administrador";
    public const string RolGestor = "Gestor";
}
