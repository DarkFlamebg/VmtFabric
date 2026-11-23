using FluentValidation;
using VmtFabric.Api.Models.DTOs.Auth;

namespace VmtFabric.Api.Validators;

/// <summary>En el login no se aplica la política de contraseña: solo se limita el tamaño.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Correo).NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(ReglasComunes.MaxCorreo);
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(128);
    }
}
