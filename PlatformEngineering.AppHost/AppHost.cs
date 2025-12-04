
var builder = DistributedApplication.CreateBuilder(args);


// Add app
var app = builder.AddViteApp("app", "../app")
    .WithPnpm()
    .WithExternalHttpEndpoints();

// Add API service with reference to app for CORS service discovery
var api = builder.AddProject<Projects.api>("api")
                 .WithReference(app)
                 .WithExternalHttpEndpoints();

// Configure app to reference API
app.WithReference(api)
   .WaitFor(api)
   .WithEnvironment("NUXT_API_URL", api.GetEndpoint("https"));


builder.Build().Run();
