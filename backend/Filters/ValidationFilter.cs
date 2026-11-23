using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VmtFabric.Api.Filters;

/// <summary>
/// Ejecuta el validador FluentValidation de cada argumento de la acción (si existe) antes del
/// controller. Si hay errores responde 400 ValidationProblemDetails, con el mismo formato que [ApiController].
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null)
                continue;

            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (services.GetService(tipoValidador) is not IValidator validador)
                continue;

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
            // Claves en camelCase, igual que el JSON de la API ("rolId"), para asociarlas al campo del formulario.
            foreach (var error in resultado.Errors)
                context.ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            var controller = (ControllerBase)context.Controller;
            var problem = controller.ProblemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest,
                title: "Uno o más datos no son válidos.");
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
