using System.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Data;

namespace VmtFabric.Api.Extensions;

/// <summary>Registro de servicios agrupado por responsabilidad para mantener Program.cs corto.</summary>
public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "Frontend";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // Fallar al arrancar si falta la cadena: en producción se entrega por variable de entorno
        // (ConnectionStrings__VmtFabric) o un gestor de secretos, nunca en el repositorio.
        var connectionString = config.GetConnectionString("VmtFabric");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:VmtFabric'.");

        services.AddDbContext<VmtFabricDbContext>(opt =>
        {
            opt.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3));
            // Los valores de parámetros solo se registran en desarrollo (pueden contener datos personales).
            if (env.IsDevelopment())
                opt.EnableSensitiveDataLogging().EnableDetailedErrors();
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sqlserver");
        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
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
