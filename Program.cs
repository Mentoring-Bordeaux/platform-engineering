using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the API service
var api = builder.AddProject<Projects.api>("api");

// Add Nuxt app
var app = builder.AddPnpmApp("app", "app", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(api)
    .WithEnvironment(context =>
    {
        var endpoint = api.GetEndpoint("http");
        context.EnvironmentVariables["NUXT_API_URL"] = endpoint.Url;
    });
;

builder.Build().Run();
