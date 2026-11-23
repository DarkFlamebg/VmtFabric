using VmtFabric.Api.Exceptions;
using VmtFabric.Api.Security;

namespace VmtFabric.Api.Middleware;

/// <summary>
/// Si el token indica contraseña temporal (claim pwd_change), solo deja pasar los endpoints
/// marcados con <see cref="PermitirConPasswordPendienteAttribute"/>. Va después de UseAuthentication.
/// </summary>
public sealed class PasswordPendienteMiddleware(RequestDelegate next)
{
    public const string Codigo = "DEBE_CAMBIAR_PASSWORD";

    public Task InvokeAsync(HttpContext context)
    {
        var pendiente = context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim(ClaimNames.DebeCambiarPassword, "true");

        if (pendiente && context.GetEndpoint()?.Metadata.GetMetadata<PermitirConPasswordPendienteAttribute>() is null)
            throw new ForbiddenException("Debe cambiar su contraseña temporal antes de continuar.", Codigo);

        return next(context);
    }
}
