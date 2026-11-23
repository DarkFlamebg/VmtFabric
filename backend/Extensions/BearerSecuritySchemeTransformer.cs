using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace VmtFabric.Api.Extensions;

/// <summary>Declara el esquema "Bearer" en el documento OpenAPI para que Scalar permita enviar el token.</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token de POST /api/auth/login"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, document)] = []
        });
        return Task.CompletedTask;
    }
}
