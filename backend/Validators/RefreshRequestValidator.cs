using FluentValidation;
using VmtFabric.Api.Models.DTOs.Auth;

namespace VmtFabric.Api.Validators;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("El refresh token es obligatorio.")
            .MaximumLength(200);
    }
}
