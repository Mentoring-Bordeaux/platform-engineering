#:package CommunityToolkit.Aspire.Hosting.Azure.DataApiBuilder@13.0.0
#:sdk Aspire.AppHost.Sdk@13.0.1
// #:sdk Aspire.Hosting.JavaScript

var builder = DistributedApplication.CreateBuilder(args);

// Add API service
var api = builder.AddProject("api", "api/api.csproj");

// var app = builder.AddViteApp("app", packageManager: "pnpm").WithReference(api).WithEnvironment(context =>
//     {
//         var endpoint = api.GetEndpoint("http");
//         context.EnvironmentVariables["NUXT_API_URL"] = endpoint.Url;
//     });

builder.Build().Run();
