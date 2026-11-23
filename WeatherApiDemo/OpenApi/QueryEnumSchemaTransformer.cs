using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WeatherApiDemo.Server.Weather.Api;

namespace WeatherApiDemo.Server.OpenApi;

/// <summary>Documents <see cref="QueryEnum{TEnum}"/> as the plain string enum it is bound from.</summary>
internal sealed class QueryEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public static bool IsQueryEnum(Type type) => EnumTypeOf(type) is not null;

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (EnumTypeOf(context.JsonTypeInfo.Type) is { } enumType)
        {
            schema.Type = JsonSchemaType.String;
            schema.Properties = null;
            schema.Description = null;
            schema.Enum = Enum.GetNames(enumType).Select(name => (JsonNode)JsonValue.Create(name)).ToList();
        }

        return Task.CompletedTask;
    }

    private static Type? EnumTypeOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(QueryEnum<>)
            ? type.GetGenericArguments()[0]
            : null;
    }
}
