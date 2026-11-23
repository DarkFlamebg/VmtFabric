using FluentValidation;
using VmtFabric.Api.Models.DTOs.Usuarios;

namespace VmtFabric.Api.Validators;

public sealed class ActualizarUsuarioRequestValidator : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre).NombreUsuario();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.RolId).RolValido();
    }
}
