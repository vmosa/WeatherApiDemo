var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WeatherApiDemo>("weatherapidemo");

builder.Build().Run();
