using Pulumi.Automation;
using System.Text.RegularExpressions;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Configure CORS to allow requests from Nuxt 3 development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy",
        policy =>
        {
            // Retrieve the Nuxt app URL from environment variables and allow it
            var nuxtAppUrl = builder.Configuration["NUXT_APP_URL"] ?? "http://localhost:3000"; // Can't be null
            policy.WithOrigins(nuxtAppUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            
        });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Use the configured CORS policy
app.UseCors("NuxtPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

async Task<IResult> CreateStaticWebapp(StaticWebSiteRequest request)
{

    var stackName = "storage-staticweb-stack" + Regex.Replace(request.Name.ToLower(), @"[^a-z0-9\-]", "-");

    var executingDir = Directory.GetCurrentDirectory();
    var workingDir = Path.Combine(executingDir, "pulumiPrograms", "staticWebApp");

    try
    {
        var stackArgs = new LocalProgramArgs(stackName, workingDir);
        var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        
        await stack.SetConfigAsync("Name", new ConfigValue(request.Name));

        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.Error.WriteLine
        });

        var outputs = result.Outputs.ToDictionary(
            kv => kv.Key, 
            kv => kv.Value?.Value
        );
        return TypedResults.Json(outputs, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        return TypedResults.Json(
            new { message = "Erreur" },
            statusCode: 500
        );
    }
}



app.MapPost("/create-staticweb", async (StaticWebSiteRequest request) =>
{
    return await CreateStaticWebapp(request);
});

app.Run();
