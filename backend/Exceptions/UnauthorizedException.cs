namespace VmtFabric.Api.Exceptions;

public class UnauthorizedException(string message)
    : AppException(message, StatusCodes.Status401Unauthorized, "No autorizado");
