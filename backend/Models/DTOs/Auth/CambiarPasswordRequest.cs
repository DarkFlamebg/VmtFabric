namespace VmtFabric.Api.Models.DTOs.Auth;

public sealed record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);
