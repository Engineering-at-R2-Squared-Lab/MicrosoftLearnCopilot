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