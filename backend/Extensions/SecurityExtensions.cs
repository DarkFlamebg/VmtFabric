using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VmtFabric.Api.Configuration;
using VmtFabric.Api.Security;

namespace VmtFabric.Api.Extensions;

/// <summary>Autenticación JWT, autorización por rol y límite de peticiones (fase 02).</summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddSeguridad(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<JwtSettings>()
            .Bind(config.GetSection(JwtSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();   // sin Jwt:Key (>= 32 caracteres) la API no arranca

        services.AddOptions<LoginSettings>()
            .Bind(config.GetSection(LoginSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((o, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                o.MapInboundClaims = false;   // los claims conservan sus nombres cortos (sub, role...)
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.ClaveFirma(jwt.Key),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Nombre,
                    RoleClaimType = ClaimNames.Rol
                };
            });

        // S-01: todo endpoint exige sesión salvo [AllowAnonymous]. S-02: políticas por rol.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Politicas.Administrador, p => p.RequireRole(Politicas.RolAdministrador))
            .AddPolicy(Politicas.Talento, p => p.RequireRole(Politicas.RolAdministrador, Politicas.RolGestor));

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Politicas.LimiteAuth, ctx =>
            {
                var limite = ctx.RequestServices.GetRequiredService<IOptions<LoginSettings>>().Value.PeticionesPorMinuto;
                return RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
            o.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                var problems = ctx.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = ctx.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiadas peticiones",
                        Detail = "Demasiados intentos. Espere un minuto antes de volver a intentar."
                    }
                });
            };
        });

        return services;
    }
}
