namespace VmtFabric.Api.Exceptions;

public class ForbiddenException(string message, string? codigo = null)
    : AppException(message, StatusCodes.Status403Forbidden, "Acceso denegado", codigo);
