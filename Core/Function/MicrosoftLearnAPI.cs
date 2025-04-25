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
                    products = lpItems["products"]?.Select(p => p.ToString()).ToList() ?? new List<string>()
                }
            );


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
                .Select(r => allItems?.ToList()[r.Index])
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

}
