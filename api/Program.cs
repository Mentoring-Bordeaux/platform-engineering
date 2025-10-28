using System.Reflection.Metadata.Ecma335;
using Octokit;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Configure CORS to allow requests from Nuxt 4 development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy",
        policy =>
        {
            // Retrieve the Nuxt app URL from environment variables and allow it
            var nuxtAppUrl = builder.Configuration["NUXT_APP_URL"] ?? "http://localhost:3000"; // Can't be null  (TODO: throw error if null?)
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

// Method to create a GitHub repository using Octokit
async Task<Repository> CreateGitHubRepository(
    string pat,
    string repoName,
    string? description,
    bool isPrivate)
{
    // Initialize the GitHub client with the provided PAT
    var client = new GitHubClient(new ProductHeaderValue("MonAppGitHubCreator"))
    {
        Credentials = new Credentials(pat)
    };

    // Create the repository object with the specified parameters
    var newRepo = new NewRepository(repoName)
    {
        Description = description ?? "Dépôt créé via une API .NET",
        Private = isPrivate,
        AutoInit = true // Crée un README initial
    };

    // Call the GitHub API to create the repository 
    // This method will return the created repository details or throw an exception if it fails
    return await client.Repository.Create("LorkuiOrga", newRepo);
}
+




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Define the request model for creating a repository
app.MapPost("/create-repo", async (CreateRepoRequest request) =>
{
    if (string.IsNullOrEmpty(request.Pat) || string.IsNullOrEmpty(request.RepoName))
    {
        return Results.BadRequest("The 'Pat' and 'RepoName' fields are required.");
    }

    try
    {
        // Call the method to create the repository
        var repository = await CreateGitHubRepository(
            request.Pat,
            request.RepoName,
            request.Description,
            request.Private
        );
        
        // Treat the response
        return Results.Created(
            repository.HtmlUrl, // URL of the created repository
            new { 
                Message = $"Repository '{repository.Name}' created successfully.", 
                Url = repository.HtmlUrl 
            }
        );
    }
    catch (ApiException apiEx)
    {
        // Handle GitHub API errors specifically (e.g., repository name already taken)
        return Results.Problem(
            title: "GitHub API Error", 
            detail: $"Creation failed: {apiEx.Message}",
            statusCode: (int)apiEx.StatusCode
        );
    }
    catch (Exception ex)
    {
        // Handle all other exceptions
        return Results.Problem($"Internal error: {ex.Message}");
    }
});







app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


