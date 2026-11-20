using Scalar.AspNetCore;
using VmtFabric.Api.Data;
using VmtFabric.Api.Middleware;

namespace VmtFabric.Api.Extensions;

/// <summary>Pipeline HTTP. El orden de los middleware importa: no reordenar sin revisar.</summary>
public static partial class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // 0. Detrás de un proxy (Render) toma el esquema https y la IP real del cliente.
        //    Va primero para que todo lo demás vea los valores correctos.
        if (app.Configuration.GetValue<bool>(ServiceCollectionExtensions.ReverseProxyEnabledKey))
            app.UseForwardedHeaders();

        app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. captura errores de todo lo que sigue
        app.UseMiddleware<SecurityHeadersMiddleware>();     // 2. cabeceras en toda respuesta
        app.UseStatusCodePages();                           // 3. 404/405 vacíos -> ProblemDetails

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();                                          // /openapi/v1.json
            app.MapScalarApiReference(o => o.Title = "VmtFabric API"); // /scalar
        }
        else
        {
            // La documentación no se publica en producción.
            app.UseHsts();
            // /health queda fuera para que el chequeo interno del proveedor (HTTP) no reciba un 307.
            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/health"),
                branch => branch.UseHttpsRedirection());
        }

        app.UseCors(ServiceCollectionExtensions.CorsPolicy);
        // Fase 02: app.UseAuthentication(); app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");
        return app;
    }

    /// <summary>En desarrollo construye el modelo EF al arrancar: un error de mapeo falla de inmediato.</summary>
    public static WebApplication ValidateEfModel(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        using var scope = app.Services.CreateScope();
        var model = scope.ServiceProvider.GetRequiredService<VmtFabricDbContext>().Model;
        var entidades = model.GetEntityTypes().Count();
        LogModeloValidado(app.Logger, entidades);
        return app;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Modelo EF validado: {Count} entidades mapeadas")]
    private static partial void LogModeloValidado(ILogger logger, int count);
}
