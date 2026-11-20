namespace VmtFabric.Api.Exceptions;

public class NotFoundException(string message)
    : AppException(message, StatusCodes.Status404NotFound, "Recurso no encontrado");
