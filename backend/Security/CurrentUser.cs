using System.Globalization;
using VmtFabric.Api.Exceptions;

namespace VmtFabric.Api.Security;

/// <summary>Adaptador sobre HttpContext: es el único punto de la capa de negocio que lo toca.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int? UsuarioId =>
        int.TryParse(accessor.HttpContext?.User.FindFirst(ClaimNames.UsuarioId)?.Value,
            NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;

    public bool EstaAutenticado => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? Ip => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public int UsuarioIdRequerido => UsuarioId ?? throw new UnauthorizedException("Sesión no válida.");
}
