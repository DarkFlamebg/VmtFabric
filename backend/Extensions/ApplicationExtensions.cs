using FluentValidation;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Data.Interceptors;
using VmtFabric.Api.Security;
using VmtFabric.Api.Services.Auditoria;
using VmtFabric.Api.Services.Auth;
using VmtFabric.Api.Services.Usuarios;
using VmtFabric.Api.Validators;

namespace VmtFabric.Api.Extensions;

/// <summary>Servicios de negocio, validadores y piezas transversales (M-03: interfaz → implementación).</summary>
public static class ApplicationExtensions
{
    public static IServiceCollection AddAplicacion(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        // Seguridad
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.Configure<AdminInicialSettings>(config.GetSection(AdminInicialSettings.Section));
        services.AddScoped<AdminInicialService>();

        // Auditoría
        services.AddScoped<AuditoriaInterceptor>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        // Módulos
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        // Validación: al primer error de una propiedad no se evalúan sus demás reglas.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Singleton);

        return services;
    }
}
