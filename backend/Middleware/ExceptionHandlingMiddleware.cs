using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VmtFabric.Api.Exceptions;

namespace VmtFabric.Api.Middleware;

/// <summary>
/// Captura cualquier excepción y responde con ProblemDetails (RFC 9457).
/// Seguridad: los errores 500 nunca exponen el mensaje ni el stack trace;
/// el cliente recibe un traceId para correlacionar con el log del servidor.
/// </summary>
public partial class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetails,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no es un error del servidor.
            LogCancelada(logger, context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                LogRespuestaIniciada(logger, ex, context.Request.Path);
                throw;
            }
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = Map(ex);

        if (status >= StatusCodes.Status500InternalServerError)
            LogErrorNoControlado(logger, ex, context.Request.Method, context.Request.Path);
        else
            LogErrorControlado(logger, status, title, context.Request.Method, context.Request.Path, detail);

        context.Response.Clear();
        context.Response.StatusCode = status;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception ex) => ex switch
    {
        AppException app => (app.StatusCode, app.Title, app.Message),

        // 2601/2627: índice o restricción UNIQUE (cédula, correo, asignación activa...)
        DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
            (StatusCodes.Status409Conflict, "Conflicto", "Ya existe un registro con esos datos."),

        // 547: violación de FK o CHECK
        DbUpdateException { InnerException: SqlException { Number: 547 } } =>
            (StatusCodes.Status409Conflict, "Conflicto", "La operación viola una restricción de integridad."),

        DbUpdateConcurrencyException =>
            (StatusCodes.Status409Conflict, "Conflicto", "El registro fue modificado por otro usuario."),

        BadHttpRequestException bad =>
            (bad.StatusCode, "Petición inválida", "La petición no es válida."),

        _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
    };

    // Logs con source generator: sin costo cuando el nivel está deshabilitado (CA1848).
    [LoggerMessage(Level = LogLevel.Debug, Message = "Petición cancelada por el cliente: {Path}")]
    private static partial void LogCancelada(ILogger logger, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Excepción después de iniciar la respuesta en {Path}")]
    private static partial void LogRespuestaIniciada(ILogger logger, Exception ex, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Method} {Path}")]
    private static partial void LogErrorNoControlado(ILogger logger, Exception ex, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Status} {Title} en {Method} {Path}: {Detail}")]
    private static partial void LogErrorControlado(ILogger logger, int status, string title, string method, PathString path, string detail);
}
