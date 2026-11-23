using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi;
using WeatherApiDemo.Server.Security;

namespace WeatherApiDemo.Server.OpenApi;

/// <summary>
/// Declares the JWT bearer scheme and, while the JwtAuthentication flag is on, applies it to protected endpoints.
/// </summary>
internal sealed class BearerSecurityTransformer(IFeatureManager featureManager)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT bearer token."
        };

        return Task.CompletedTask;
    }

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (requiresAuthorization && await featureManager.IsEnabledAsync(FeatureFlags.JwtAuthentication))
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
            });
        }
    }
}
