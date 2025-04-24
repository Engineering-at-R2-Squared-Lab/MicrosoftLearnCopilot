using System.ComponentModel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using MicrosoftLearnCopilot.Core.Model;
namespace MicrosoftLearnCopilot.Core.Function;

public class MicrosoftLearnAPI
{
    private HttpClient httpClient = new HttpClient();

    [KernelFunction("getLearningPath")]
    [Description("Get Learning Path")]
    public async Task<List<MicrosoftLearnModel.LearningPathItem>> getLearningPath()
    {
        var url = "https://learn.microsoft.com/api/catalog/?type=learningPaths";
        var response = await httpClient.GetAsync(url);
        Console.WriteLine(response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(content);
            JArray lp = (JArray)root["learningPaths"];

            var filteredLP = lp?.Take(5).Select(lp => new MicrosoftLearnModel.LearningPathItem
            {
                summary = lp["summary"]?.ToString() ?? string.Empty,
                title = lp["title"]?.ToString() ?? string.Empty,
                url = lp["url"]?.ToString() ?? string.Empty,
            }).ToList();

            Console.WriteLine(lp.ToString());

            return filteredLP ?? new List<MicrosoftLearnModel.LearningPathItem>();

            throw new NotImplementedException("Not implemented yet.");

        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return null;
        }
    }


}
