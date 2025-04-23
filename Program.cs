using dotenv.net;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
// See https://aka.ms/new-console-template for more information
DotEnv.Load();

var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
deploymentName: Environment.GetEnvironmentVariable("deploymentName") ?? string.Empty,
    endpoint: Environment.GetEnvironmentVariable("endpoint") ?? string.Empty,
    apiKey: Environment.GetEnvironmentVariable("apiKey") ?? string.Empty
);


Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();
string? userInput;

do
{
    Console.WriteLine("Enter your message (or 'exit' to quit): ");
    userInput = Console.ReadLine();
    if (userInput == "exit") break;
    history.AddUserMessage(userInput?? string.Empty);
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history
    );
    Console.WriteLine($"AI: {result}");
    history.AddAssistantMessage(result.Content.ToString());

}
while (true);