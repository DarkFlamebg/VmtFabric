namespace VmtFabric.Api.Models.DTOs.Auth;

/// <summary>Datos del usuario para el header del frontend (GET /api/auth/me).</summary>
public sealed record UsuarioActualDto(int UsuarioId, string Nombre, string Correo, string Rol, bool DebeCambiarPassword);
