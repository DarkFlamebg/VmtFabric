namespace VmtFabric.Api.Security;

/// <summary>Usuario de la petición actual. Los servicios lo usan sin conocer HttpContext (guía, sección 1).</summary>
public interface ICurrentUser
{
    int? UsuarioId { get; }
    bool EstaAutenticado { get; }
    string? Ip { get; }

    /// <summary>Id del usuario autenticado; lanza 401 si no hay sesión.</summary>
    int UsuarioIdRequerido { get; }
}
