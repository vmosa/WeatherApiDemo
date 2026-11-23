using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using WeatherApiDemo.Server;
using WeatherApiDemo.Server.OpenApi;
using WeatherApiDemo.Server.Security;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Api;
using WeatherApiDemo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddWeatherAggregation();
builder.AddApiSecurity();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
    options.AddSchemaTransformer<QueryEnumSchemaTransformer>();
    options.CreateSchemaReferenceId = type => QueryEnumSchemaTransformer.IsQueryEnum(type.Type)
        ? null
        : OpenApiOptions.CreateDefaultSchemaReferenceId(type);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseApiSecurity();

app.MapDefaultEndpoints();
if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PlaygroundApp v1"));
}

var api = app.MapApi();
api.MapWeatherEndpoints();
api.MapStatisticsEndpoints();

app.Run();
