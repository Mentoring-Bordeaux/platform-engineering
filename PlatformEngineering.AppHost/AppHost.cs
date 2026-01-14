var builder = DistributedApplication.CreateBuilder(args);

// Add app
var app = builder.AddViteApp("app", "../app").WithPnpm().WithExternalHttpEndpoints();

// Add API service with reference to app for CORS service discovery
var api = builder.AddProject<Projects.api>("api").WithReference(app).WithExternalHttpEndpoints();

builder.Build().Run();
