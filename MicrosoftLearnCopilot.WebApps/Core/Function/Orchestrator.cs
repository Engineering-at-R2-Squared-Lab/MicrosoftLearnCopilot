using MicrosoftLearnCopilot.Core.Model;
using MicrosoftLearnCopilot.Core.Function;
using MicrosoftLearnCopilot.Core;
using System.Text.RegularExpressions;

namespace MicrosoftLearnCopilot.Core.Function;

public class MicrosoftLearnOrchestrator
{
    private readonly MicrosoftLearnAPI api = new MicrosoftLearnAPI();

    /// <summary>
    /// Orchestrates the full scenario: query -> learning paths -> modules -> units -> LLM answer.
    /// </summary>
    public async Task<List<LearningPathResult>> GetLearningPathsWithModulesAndUnits(string query)
    {
        var learningPaths = await api.getLearningPath(query);
        var result = new List<LearningPathResult>();

        foreach (var lp in learningPaths)
        {
            var modules = new List<ModuleResult>();
            foreach (var moduleUid in lp.modules)
            {
                var moduleItems = await api.getModules(moduleUid);
                foreach (var module in moduleItems)
                {
                    var units = await api.getUnitsForModule(module.uid);
                    modules.Add(new ModuleResult
                    {
                        Module = module,
                        Units = units
                    });
                }
            }
            result.Add(new LearningPathResult
            {
                LearningPath = lp,
                Modules = modules
            });
        }
        return result;
    }

    /// <summary>
    /// Fetches the HTML/text content of a unit given its URL.
    /// </summary>
    public async Task<string> GetUnitContent(string unitUrl)
    {
        using var httpClient = new HttpClient();
        Console.WriteLine($"Fetching content from: {unitUrl}");
        var response = await httpClient.GetAsync(unitUrl);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            Console.WriteLine($"Error fetching unit content: {response.StatusCode}");
            return string.Empty;
        }
    }

    public async Task<string> FetchCombinedUnitContent(List<LearningPathResult> learningPaths)
    {
        var unitUrls = new List<string>();
        var allUnitContents = new List<string>();

        var firstLp = learningPaths.First();
        var firstModule = firstLp.Modules.FirstOrDefault();
        if (firstModule == null || firstModule.Units.Count == 0)
        {
            Console.WriteLine("No modules or units found for the learning path.");
            return string.Empty;
        }

        // Extract base URL from first unit
        var firstUnitUrl = firstModule.Module.firstUnitUrl;
        var lastSlashIndex = firstUnitUrl.LastIndexOf('/');
        var moduleBaseUrl = firstUnitUrl.Substring(0, lastSlashIndex);
        moduleBaseUrl = moduleBaseUrl.Substring(0, moduleBaseUrl.LastIndexOf('/'));

        // Process each unit
        for (int i = 0; i < firstModule.Units.Count; i++)
        {
            var unitUid = firstModule.Units[i].uid;
            var lastPart = unitUid.Split('.').Last().Replace('_', '-');

            // Try to use previous URL pattern if available
            if (unitUrls.Count > 0)
            {
                var previousUrl = unitUrls.Last();
                var lastSlashInPrevUrl = previousUrl.LastIndexOf('/');
                var baseUrl = previousUrl.Substring(0, lastSlashInPrevUrl);
                var newUrl = $"{baseUrl}/{lastPart}";

                var unitContent = await GetUnitContent(newUrl);
                if (!string.IsNullOrWhiteSpace(unitContent))
                {
                    unitUrls.Add(newUrl);
                    allUnitContents.Add(unitContent);
                    continue;
                }
                else
                {
                    Console.WriteLine($"Previous pattern failed, fallback to TryUnitUrlFormats...");
                }
            }

            // Try various URL formats
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

        // Combine all unit contents with double newline separator
        var combinedContent = string.Join("\n\n", allUnitContents);
        return combinedContent;
    }

    // Helper function to try different URL format patterns
    private async Task<(string? url, string? content)> TryUnitUrlFormats(string baseUrl, string slug, int idx)
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
            var content = await GetUnitContent(url);
            if (!string.IsNullOrWhiteSpace(content))
                return (url, content);
        }

        // Try without prefix
        var plainUrl = $"{baseUrl}/{slug}";
        Console.WriteLine($"Trying: {plainUrl}");
        var plainContent = await GetUnitContent(plainUrl);
        if (!string.IsNullOrWhiteSpace(plainContent))
            return (plainUrl, plainContent);

        return (null, null);
    }

    /// <summary>
    /// Calls the LLM to summarize or answer based on unit content.
    /// </summary>
    public async Task<string> SummarizeOrAnswerFromUnit(string userQuestion, string unitContent, KernelService kernelService)
    {
        // Optionally, strip HTML tags for a cleaner prompt
        string plainText = Regex.Replace(unitContent, "<.*?>", string.Empty);

        string prompt = $@"
You are a helpful assistant. Based on the following Microsoft Learn content, answer the user's question.

User question: {userQuestion}

Content:
{plainText}

Answer:
";
        string llmResponse = await kernelService.chatCompletion(prompt);
        return llmResponse;
    }
}

public class LearningPathResult
{
    public required MicrosoftLearnModel.LearningPathItem LearningPath { get; set; }
    public required List<ModuleResult> Modules { get; set; }
}

public class ModuleResult
{
    public required MicrosoftLearnModel.ModuleItem Module { get; set; }
    public required List<MicrosoftLearnModel.UnitItem> Units { get; set; }
}