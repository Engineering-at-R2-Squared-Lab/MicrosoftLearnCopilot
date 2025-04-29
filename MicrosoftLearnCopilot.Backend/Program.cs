// using MicrosoftLearnCopilot;
using MongoDB.Driver;
using dotenv.net;
using MicrosoftLearnCopilot.Core;
using MicrosoftLearnCopilot.Core.Function;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MicrosoftLearnCopilot.Backend.Model;
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
// var mongoClient = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnectionString") ?? "");
MongoClientSettings settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("MongoDBConnectionString"));
var mongoClient = new MongoClient(settings);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();




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
    var collection = mongoClient.GetDatabase("history").GetCollection<ChatHistoryModel>("session");
    var kernel = new KernelService(
    deploymentName: Environment.GetEnvironmentVariable("deploymentName") ?? "",
    apiKey: Environment.GetEnvironmentVariable("apiKey") ?? "",
    endpoint: Environment.GetEnvironmentVariable("endpoint") ?? "");

    if (string.IsNullOrEmpty(sessionId) == false)
    {
        var filter = collection.Find<ChatHistoryModel>(sessionId).FirstOrDefault();
        kernel.setChatHistory(filter.history);
    }

    if (string.IsNullOrEmpty(query))
    {
        return Results.BadRequest("Query cannot be empty.");
    }

    var msg = await kernel.chatCompletion(query);


    var result = new
    {
        query = query,
        response = msg,
        sessionId = sessionId
    };
    return Results.Ok();
}).WithName("Invoke AI Agents");

app.Run();

