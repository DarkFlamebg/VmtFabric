namespace VmtFabric.Api.Exceptions;

/// <summary>Violación de una regla de negocio (RN-xx).</summary>
public class BusinessRuleException(string message)
    : AppException(message, StatusCodes.Status422UnprocessableEntity, "Regla de negocio");
