namespace VmtFabric.Api.Exceptions;

public class ForbiddenException(string message)
    : AppException(message, StatusCodes.Status403Forbidden, "Acceso denegado");
