using DotNetEnv;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy", policy =>
    {
        var nuxtAppUrl = Environment.GetEnvironmentVariable("NUXT_APP_URL") ?? "http://localhost:3001";
        policy.WithOrigins(nuxtAppUrl).AllowAnyHeader().AllowAnyMethod();
    });
});

// Register the generic Pulumi service
builder.Services.AddScoped<PulumiService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("NuxtPolicy");

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();


app.MapPost("/create-resource", async (TemplateRequest request, PulumiService service) =>
{
    if (string.IsNullOrEmpty(request.Name))
        return Results.BadRequest("The 'Name' field is required.");
    if (!request.Parameters.ContainsKey("githubToken"))
    {
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(githubToken))
            request.Parameters["githubToken"] = githubToken;
    }

    return await service.ExecuteAsync(request);
});

app.MapPost("/create-project", async (TemplateRequest[] request, PulumiService service) =>
{
    List<IResult> results = new List<IResult>();
    List<string> jsonResults = new List<string>();
    foreach (TemplateRequest req in request)
    {
        
        if (string.IsNullOrEmpty(req.Name))
        {
            results.Add(Results.BadRequest("The 'Name' field is required."));
            continue;
        }
        if (!req.Parameters.ContainsKey("githubToken"))
        {
            string githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(githubToken))
                req.Parameters["githubToken"] = githubToken;
        }

        foreach (var kv in req.Parameters)
        {
            Console.WriteLine($"Parameter: {kv.Key} = {kv.Value}");
        }
        Console.WriteLine("Executing Pulumi service...");
        IResult result = await service.ExecuteAsync(req);

        Console.WriteLine($"Type of IResult : {result}");
        if (result is Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<Dictionary<string, object>> jsonResult)
        {
            // Sérialise le Dictionnaire en une chaîne JSON formatée pour la lecture
            var jsonString = JsonSerializer.Serialize(jsonResult.Value, new JsonSerializerOptions { WriteIndented = true });
            
            Console.WriteLine("--- Contenu JSON sérialisé ---");
            Console.WriteLine(jsonString);
            Console.WriteLine("------------------------------");
            jsonResults.Add(jsonString);

        }

        results.Add(result);
    }
    Console.WriteLine("Final aggregated results prepared.");
    Console.WriteLine("--- Aggregated JSON Results ---");
    foreach (var json in jsonResults)
    {
        Console.WriteLine(json);
    }
    Console.WriteLine("-------------------------------");
    return results;
});

app.Run();
