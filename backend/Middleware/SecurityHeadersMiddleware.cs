namespace VmtFabric.Api.Middleware;

/// <summary>
/// Agrega cabeceras de seguridad HTTP a todas las respuestas (recomendaciones OWASP para APIs).
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    // La UI de documentación (Scalar) necesita scripts y estilos; el resto de la API no sirve HTML.
    private static readonly PathString DocsPath = new("/scalar");

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        if (!context.Request.Path.StartsWithSegments(DocsPath))
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Las respuestas de la API contienen datos personales: no deben quedar en caché.
        if (context.Request.Path.StartsWithSegments("/api"))
            headers.CacheControl = "no-store";

        return next(context);
    }
}
