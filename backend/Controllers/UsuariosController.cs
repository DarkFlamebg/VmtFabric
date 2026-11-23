using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VmtFabric.Api.Models.DTOs.Comun;
using VmtFabric.Api.Models.DTOs.Usuarios;
using VmtFabric.Api.Security;
using VmtFabric.Api.Services.Usuarios;

namespace VmtFabric.Api.Controllers;

/// <summary>Administración de usuarios: solo Administrador (S-02).</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.Administrador)]
[Produces("application/json")]
public sealed class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaResultado<UsuarioDto>>(StatusCodes.Status200OK)]
    public Task<PaginaResultado<UsuarioDto>> Listar([FromQuery] UsuarioFiltro filtro, CancellationToken ct) =>
        usuarios.ListarAsync(filtro, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UsuarioDto> Obtener(int id, CancellationToken ct) => usuarios.ObtenerAsync(id, ct);

    /// <summary>Crea el usuario con una contraseña temporal que se devuelve una sola vez.</summary>
    [HttpPost]
    [ProducesResponseType<PasswordTemporalResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var creado = await usuarios.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Usuario.UsuarioId }, creado);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<UsuarioDto> Actualizar(int id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        usuarios.ActualizarAsync(id, request, ct);

    /// <summary>Activa o desactiva el usuario (no se eliminan usuarios: quedan en la auditoría).</summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<UsuarioDto> CambiarEstado(int id, CambiarEstadoUsuarioRequest request, CancellationToken ct) =>
        usuarios.CambiarEstadoAsync(id, request.Activo, ct);

    /// <summary>Genera una contraseña temporal, desbloquea la cuenta y cierra sus sesiones.</summary>
    [HttpPost("{id:int}/restablecer-password")]
    [ProducesResponseType<PasswordTemporalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<PasswordTemporalResponse> RestablecerPassword(int id, CancellationToken ct) =>
        usuarios.RestablecerPasswordAsync(id, ct);

    [HttpGet("roles")]
    [ProducesResponseType<IReadOnlyList<RolDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RolDto>> Roles(CancellationToken ct) => usuarios.ListarRolesAsync(ct);
}
