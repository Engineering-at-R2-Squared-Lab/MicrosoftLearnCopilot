using dotenv.net;
using MicrosoftLearnCopilot.Core;
using MicrosoftLearnCopilot.Core.Function;

DotEnv.Load();

var kernel = new KernelService(
    deploymentName: Environment.GetEnvironmentVariable("deploymentName") ?? "",
    apiKey: Environment.GetEnvironmentVariable("apiKey") ?? "",
    endpoint: Environment.GetEnvironmentVariable("endpoint") ?? ""
);

var orchestrator = new MicrosoftLearnOrchestrator();

do
{
    Console.Write("Enter your prompt (or 'exit' to quit): ");
    string prompt = Console.ReadLine() ?? "";

    if (prompt.ToLower() == "exit" || string.IsNullOrEmpty(prompt))
    {
        Console.WriteLine("Exiting...");
        break;
    }

    // Get learning paths, modules, units
    var results = await orchestrator.GetLearningPathsWithModulesAndUnits(prompt);

    if (results.Count == 0)
    {
        Console.WriteLine("No relevant learning paths found.");
        continue;
    }

    // For demo, pick the first module of the first learning path
    var firstLp = results.First();
    var firstModule = firstLp.Modules.FirstOrDefault();

    if (firstModule == null || firstModule.Units.Count == 0)
    {
        Console.WriteLine("No modules or units found for the top learning path.");
        continue;
    }

    // Construct URLs for all units
    var firstUnitUrl = firstModule.Module.firstUnitUrl;
    var lastSlashIndex = firstUnitUrl.LastIndexOf('/');
    var moduleBaseUrl = firstUnitUrl.Substring(0, lastSlashIndex);
    moduleBaseUrl = moduleBaseUrl.Substring(0, moduleBaseUrl.LastIndexOf('/'));

    var unitUrls = new List<string>();
    for (int i = 0; i < firstModule.Units.Count; i++)
    {
        var unitUid = firstModule.Units[i].uid;
        var lastPart = unitUid.Split('.').Last().Replace('_', '-');
        var url = $"{moduleBaseUrl}/{i + 1}-{lastPart}";
        unitUrls.Add(url);
    }

    // Fetch and concatenate all unit contents
    var allUnitContents = new List<string>();
    foreach (var url in unitUrls)
    {
        var content = await orchestrator.GetUnitContent(url);
        if (!string.IsNullOrWhiteSpace(content))
            allUnitContents.Add(content);
    }
    var combinedContent = string.Join("\n\n", allUnitContents);

    // Ask LLM to answer user's question based on all unit contents
    string llmAnswer = await orchestrator.SummarizeOrAnswerFromUnit(prompt, combinedContent, kernel);

    Console.WriteLine($"\n=== LLM Answer Based on All Units ===\n{llmAnswer}\n");

} while (true);