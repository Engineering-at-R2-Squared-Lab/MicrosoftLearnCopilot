using dotenv.net;
using MicrosoftLearnCopilot.Core;

DotEnv.Load();

var kernel = new KernelService(
    deploymentName: Environment.GetEnvironmentVariable("deploymentName") ?? "",
    apiKey: Environment.GetEnvironmentVariable("apiKey") ?? "",
    endpoint: Environment.GetEnvironmentVariable("endpoint") ?? ""
    );

do
{
    Console.Write("Enter your prompt (or 'exit' to quit): ");
    string prompt = Console.ReadLine() ?? "";

    if (prompt.ToLower() == "exit" | string.IsNullOrEmpty(prompt))
    {
        Console.WriteLine("Exiting...");
        break;
    }

    string response = await kernel.chatCompletion(prompt);
    Console.WriteLine($"Response: {response}");
} while (true);

