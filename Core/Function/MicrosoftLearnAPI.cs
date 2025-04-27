using System.ComponentModel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using MicrosoftLearnCopilot.Core.Model;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.Composite;
namespace MicrosoftLearnCopilot.Core.Function;

public class MicrosoftLearnAPI
{
    private HttpClient httpClient = new HttpClient();

    [KernelFunction("getLearningPath")]
    [Description("Get Learning Path")]
    public async Task<List<MicrosoftLearnModel.LearningPathItem>> getLearningPath(string query)
    {
        var url = "https://learn.microsoft.com/api/catalog/?type=learningPaths";
        var response = await httpClient.GetAsync(url);
        Console.WriteLine(response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(content);
            JArray lp = root["learningPaths"] as JArray ?? new JArray();

            var allItems = lp?.Select(
                lpItems => new MicrosoftLearnModel.LearningPathItem
                {
                    title = lpItems["title"]?.ToString() ?? string.Empty,
                    summary = lpItems["summary"]?.ToString() ?? string.Empty,
                    url = lpItems["url"]?.ToString() ?? string.Empty,
                    products = lpItems["products"]?.Select(p => p.ToString()).ToList() ?? new List<string>(),
                    modules = lpItems["modules"]?.Select(m => m.ToString()).ToList() ?? new List<string>(),
                    uid = lpItems["uid"]?.ToString() ?? string.Empty,
                    firstModuleUrl = lpItems["firstModuleUrl"]?.ToString() ?? string.Empty
                }
            ).ToList();

            if (string.IsNullOrEmpty(query))
            {
                return allItems?.Take(5).ToList() ?? new List<MicrosoftLearnModel.LearningPathItem>();
            }

            // Fuzzy search for the query in the title and summary of the learning paths
            var results = Process.ExtractSorted(query, allItems?.Select(
                item => $"{item.title} - {item.summary} - {item.products}"),
                s => s,
                ScorerCache.Get<WeightedRatioScorer>()
            );

            Console.WriteLine($"Found {results?.Count()} learning paths matching the query '{query}'.");

            var filteredLP = results?
                .Where(r => r.Score > 60)
                .Select(r => allItems?[r.Index])
                .Take(5)
                .ToList();

            Console.WriteLine($"Found {filteredLP?.Count} learning paths matching the query '{query}'.");

            return filteredLP?.Where(item => item != null).Cast<MicrosoftLearnModel.LearningPathItem>().ToList()
                   ?? new List<MicrosoftLearnModel.LearningPathItem>();
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return new List<MicrosoftLearnModel.LearningPathItem>();
        }
    }

    [KernelFunction("getModules")]
    [Description("Get modules for a given module UID")]
    public async Task<List<MicrosoftLearnModel.ModuleItem>> getModules(string moduleId)
    {
        var url = $"https://learn.microsoft.com/api/catalog/?type=modules&uid={moduleId}";
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(content);
            JArray modules = root["modules"] as JArray ?? new JArray();

            var moduleList = modules.Select(m => new MicrosoftLearnModel.ModuleItem
            {
                title = m["title"]?.ToString() ?? string.Empty,
                summary = m["summary"]?.ToString() ?? string.Empty,
                url = m["url"]?.ToString() ?? string.Empty,
                products = m["products"]?.Select(p => p.ToString()).ToList() ?? new List<string>(),
                units = m["units"]?.Select(u => u.ToString()).ToList() ?? new List<string>(),
                firstUnitUrl = m["firstUnitUrl"]?.ToString() ?? string.Empty,
                uid = m["uid"]?.ToString() ?? string.Empty
            }).ToList();

            return moduleList;
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return new List<MicrosoftLearnModel.ModuleItem>();
        }
    }

    [KernelFunction("getUnitsForModule")]
    [Description("Get units for a given module UID")]
    public async Task<List<MicrosoftLearnModel.UnitItem>> getUnitsForModule(string moduleUid)
    {
        var url = $"https://learn.microsoft.com/api/catalog/?type=modules&uid={moduleUid}";
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(content);
            JArray modules = root["modules"] as JArray ?? new JArray();
            var module = modules.FirstOrDefault(m => m["uid"]?.ToString() == moduleUid);

            if (module != null && module["units"] is JArray units)
            {
                var unitList = units.Select(u => new MicrosoftLearnModel.UnitItem
                {
                    uid = u.ToString(),
                }).ToList();

                return unitList;
            }
            else
            {
                return new List<MicrosoftLearnModel.UnitItem>();
            }
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return new List<MicrosoftLearnModel.UnitItem>();
        }
    }
}