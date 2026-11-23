using FluentValidation;
using VmtFabric.Api.Models.DTOs.Usuarios;

namespace VmtFabric.Api.Validators;

public sealed class UsuarioFiltroValidator : AbstractValidator<UsuarioFiltro>
{
    public const int MaxTamanoPagina = 100;

    public UsuarioFiltroValidator()
    {
        RuleFor(x => x.Buscar).MaximumLength(100);
        RuleFor(x => x.RolId!.Value).RolValido().When(x => x.RolId.HasValue);
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanoPagina).InclusiveBetween(1, MaxTamanoPagina);
    }
}
