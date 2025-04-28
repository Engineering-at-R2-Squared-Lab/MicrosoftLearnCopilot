using MicrosoftLearnCopilot;
using dotenv.net;
using MicrosoftLearnCopilot.Core;
using MicrosoftLearnCopilot.Core.Function;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

DotEnv.Load();

var kernel = new KernelService(
    deploymentName: Environment.GetEnvironmentVariable("deploymentName") ?? "",
    apiKey: Environment.GetEnvironmentVariable("apiKey") ?? "",
    endpoint: Environment.GetEnvironmentVariable("endpoint") ?? ""
);

var orchestrator = new MicrosoftLearnOrchestrator();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/invoke", async ([FromQuery] string query, [FromQuery] string? sessionId) =>
{

    if (string.IsNullOrEmpty(query))
    {
        return Results.BadRequest("Query cannot be empty.");
    }

    // var orchestratorResults = await orchestrator.GetLearningPathsWithModulesAndUnits(query);
    var msg = await kernel.chatCompletion(query);
    return Results.Ok(msg);
}).WithName("Invoke AI Agents");

app.Run();

