using Pulumi.Automation;


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

app.MapPost("/create-staticweb", async () =>
{
    var stackName = "storage-staticweb-stack";
    WorkspaceStack? stack = null;

    var executingDir = Directory.GetCurrentDirectory();
    var workingDir = Path.Combine(executingDir, "pulumiPrograms", "staticWebApp");

    try
    {
        var stackArgs = new LocalProgramArgs(stackName, workingDir);
        stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.Error.WriteLine
        });

        var endpoint = result.Outputs["staticWebsiteUrl"].Value.ToString();
        return Results.Ok(new { Url = endpoint });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erreur lors du déploiement : {ex.Message}");
    }
});

app.Run();
