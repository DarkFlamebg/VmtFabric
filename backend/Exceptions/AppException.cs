namespace VmtFabric.Api.Exceptions;

/// <summary>
/// Base de las excepciones de aplicación. El mensaje se muestra al cliente,
/// así que nunca debe contener datos internos (SQL, rutas, stack trace).
/// <see cref="Codigo"/> es opcional y estable: el frontend puede reaccionar a él sin leer el texto.
/// </summary>
public abstract class AppException(string message, int statusCode, string title, string? codigo = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string? Codigo { get; } = codigo;
}
