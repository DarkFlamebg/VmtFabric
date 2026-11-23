namespace VmtFabric.Api.Exceptions;

public class CuentaBloqueadaException(string message)
    : AppException(message, StatusCodes.Status423Locked, "Cuenta bloqueada", "CUENTA_BLOQUEADA");
