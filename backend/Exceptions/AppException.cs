namespace VmtFabric.Api.Exceptions;

/// <summary>
/// Base de las excepciones de aplicación. El mensaje se muestra al cliente,
/// así que nunca debe contener datos internos (SQL, rutas, stack trace).
/// </summary>
public abstract class AppException(string message, int statusCode, string title) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}
