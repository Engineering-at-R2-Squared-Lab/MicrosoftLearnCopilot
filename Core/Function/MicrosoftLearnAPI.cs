using Newtonsoft.Json.Linq;
namespace MicrosoftLearnCopilot.Core.Function;

public class MicrosoftLearnAPI
{
    private HttpClient httpClient = new HttpClient();

    public async Task getLearningPath(string query)
    {
        var url = "https://learn.microsoft.com/api/catalog/?type=learningPaths";
        var response = await httpClient.GetAsync(url);
        Console.WriteLine(response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            Console.WriteLine(json["learningPaths"]);
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }
    }

}
