
var builder = DistributedApplication.CreateBuilder(args);

// Add API service
var api = builder.AddProject<Projects.api>("api")
                 .WithExternalHttpEndpoints();

// Add app
var app = builder.AddViteApp("app", "../app")
    .WithPnpm()
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment(context =>
    {
        var endpoint = api.GetEndpoint("http");
        context.EnvironmentVariables["NUXT_API_URL"] = endpoint.Url;
    })
    .WithExternalHttpEndpoints();


builder.Build().Run();
