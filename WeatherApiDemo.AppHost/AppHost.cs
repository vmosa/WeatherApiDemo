using Aspire.Hosting.Dotnet;
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddDotnetProject("weatherapidemo", "..\\WeatherApiDemo\\WeatherApiDemo.Server.csproj")
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();