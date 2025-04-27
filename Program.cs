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

    var results = await orchestrator.GetLearningPathsWithModulesAndUnits(prompt);

    if (results.Count == 0)
    {
        Console.WriteLine("No relevant learning paths found.");
        continue;
    }

    var firstLp = results.First();
    var firstModule = firstLp.Modules.FirstOrDefault();

    if (firstModule == null || firstModule.Units.Count == 0)
    {
        Console.WriteLine("No modules or units found for the top learning path.");
        continue;
    }

    // Construct URLs for all units with robust prefix handling
    var firstUnitUrl = firstModule.Module.firstUnitUrl;
    var lastSlashIndex = firstUnitUrl.LastIndexOf('/');
    var moduleBaseUrl = firstUnitUrl.Substring(0, lastSlashIndex);
    moduleBaseUrl = moduleBaseUrl.Substring(0, moduleBaseUrl.LastIndexOf('/'));

    var unitUrls = new List<string>();
    var allUnitContents = new List<string>();

    // Try all possible prefixes and slug
    async Task<(string? url, string? content)> TryUnitUrlFormats(string baseUrl, string slug, int idx)
    {
        var prefixes = new[] {
        $"{idx + 1}-", // 1-based
        $"{(idx + 1).ToString("D2")}-", // 01-, 02-, etc.
        $"{idx}-", // 0-based
        $"{(idx).ToString("D2")}-", // 00-, 01-, etc.
        "0-", "00-", "1-", "01-", "2-", "4-", "6-"
    };

        foreach (var prefix in prefixes.Distinct())
        {
            var url = $"{baseUrl}/{prefix}{slug}";
            Console.WriteLine($"Trying: {url}");
            var content = await orchestrator.GetUnitContent(url);
            if (!string.IsNullOrWhiteSpace(content))
                return (url, content);
        }
        {
            var url = $"{baseUrl}/{slug}";
            Console.WriteLine($"Trying: {url}");
            var content = await orchestrator.GetUnitContent(url);
            if (!string.IsNullOrWhiteSpace(content))
                return (url, content);
        }
        return (null, null);
    }

    for (int i = 0; i < firstModule.Units.Count; i++)
    {
        var unitUid = firstModule.Units[i].uid;
        var lastPart = unitUid.Split('.').Last().Replace('_', '-');

        var (url, content) = await TryUnitUrlFormats(moduleBaseUrl, lastPart, i);
        if (!string.IsNullOrWhiteSpace(content) && url != null)
        {
            unitUrls.Add(url);
            allUnitContents.Add(content);
        }
        else
        {
            Console.WriteLine($"Failed to fetch any format for unit: {unitUid}");
        }
    }

    var combinedContent = string.Join("\n\n", allUnitContents);

    // Ask LLM to answer user's question based on all unit contents
    string llmAnswer = await orchestrator.SummarizeOrAnswerFromUnit(prompt, combinedContent, kernel);

    Console.WriteLine($"\n=== LLM Answer Based on All Units ===\n{llmAnswer}\n");

} while (true);