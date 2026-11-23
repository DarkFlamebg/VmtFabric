using System.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Data;
using VmtFabric.Api.Data.Interceptors;
using VmtFabric.Api.Filters;

namespace VmtFabric.Api.Extensions;

/// <summary>Registro de servicios agrupado por responsabilidad para mantener Program.cs corto.</summary>
public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "Frontend";

    private static readonly Dictionary<int, (string Title, string Detail)> TextosEstado = new()
    {
        [StatusCodes.Status401Unauthorized] = ("No autorizado", "Debe iniciar sesión."),
        [StatusCodes.Status403Forbidden] = ("Acceso denegado", "No tiene permisos para esta acción."),
        [StatusCodes.Status404NotFound] = ("No encontrado", "El recurso solicitado no existe."),
        [StatusCodes.Status405MethodNotAllowed] = ("Método no permitido", "El método HTTP no está permitido para este recurso.")
    };

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // Fallar al arrancar si falta la cadena: en producción se entrega por variable de entorno
        // (ConnectionStrings__VmtFabric) o un gestor de secretos, nunca en el repositorio.
        var connectionString = config.GetConnectionString("VmtFabric");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:VmtFabric'.");

        services.AddDbContext<VmtFabricDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3));
            // S-13: cada SaveChanges deja su rastro en la tabla Auditoria.
            opt.AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>());
            // Los valores de parámetros solo se registran en desarrollo (pueden contener datos personales).
            if (env.IsDevelopment())
                opt.EnableSensitiveDataLogging().EnableDetailedErrors();
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sqlserver");
        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Validación FluentValidation de los DTO antes de entrar al controller (S-04).
        services.AddControllers(o => o.Filters.Add<ValidationFilter>());
        services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
            // Respuestas generadas por el framework (401 sin token, 403 sin rol, 404 de ruta...): textos en español.
            if (ctx.ProblemDetails.Detail is null && ctx.ProblemDetails.Status is { } status
                && TextosEstado.TryGetValue(status, out var texto))
            {
                ctx.ProblemDetails.Title = texto.Title;
                ctx.ProblemDetails.Detail = texto.Detail;
            }
            ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        });
        return services;
    }

    public const string ReverseProxyEnabledKey = "ReverseProxy:Enabled";

    /// <summary>
    /// Confía en X-Forwarded-For/Proto solo si la API corre detrás de un proxy que los reescribe
    /// (Render). Si la API se expusiera directamente, un cliente podría falsificar su IP o el esquema.
    /// </summary>
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration config)
    {
        if (!config.GetValue<bool>(ReverseProxyEnabledKey))
            return services;

        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Las IP del proxy de Render no son fijas: se acepta el salto inmediato anterior.
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
            o.ForwardLimit = 1;
        });

        // El contenedor solo escucha HTTP; el HTTPS público lo termina el proxy en el 443.
        services.AddHttpsRedirection(o => o.HttpsPort = 443);
        services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(365));
        return services;
    }

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<CorsSettings>()
            .Bind(config.GetSection(CorsSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var origins = config.GetSection(CorsSettings.Section).Get<CorsSettings>()?.AllowedOrigins ?? [];
        services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
        return services;
    }
}
