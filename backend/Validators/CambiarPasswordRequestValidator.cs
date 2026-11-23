using FluentValidation;
using VmtFabric.Api.Models.DTOs.Auth;

namespace VmtFabric.Api.Validators;

public sealed class CambiarPasswordRequestValidator : AbstractValidator<CambiarPasswordRequest>
{
    public CambiarPasswordRequestValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("La contraseña actual es obligatoria.")
            .MaximumLength(128);
        RuleFor(x => x.PasswordNueva).PasswordSegura()
            .NotEqual(x => x.PasswordActual).WithMessage("La nueva contraseña debe ser distinta de la actual.");
    }
}
