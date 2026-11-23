namespace VmtFabric.Api.Services.Auditoria;

/// <summary>
/// Eventos que no son un cambio de datos (login, logout, exportar...). Los cambios de datos
/// los audita <c>AuditoriaInterceptor</c> automáticamente.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>Agrega el evento al contexto; se guarda en el próximo SaveChanges.</summary>
    void Registrar(string accion, string entidad, int? entidadId, int? usuarioId, object? detalle = null);
}
