using fetcher;

class Program
{
    static async Task Main()
    {
        using var httpClient = new HttpClient();
        var catalogClient = new LearnCatalog(httpClient, "https://learn.microsoft.com/api/catalog/");

        var modules = await catalogClient.GetModulesAsync();

        Console.WriteLine($"Total modules fetched: {modules.Count}");

        foreach (var module in modules)
        {
            Console.WriteLine($"Title: {module.Title}");
            Console.WriteLine($"Summary: {module.Summary}");
            Console.WriteLine($"Duration: {module.DurationInMinutes} minutes");
            Console.WriteLine($"Rating: {module.Rating?.Average} ({module.Rating?.Count} reviews)");
            Console.WriteLine($"URL: {module.Url}");
            Console.WriteLine(new string('-', 50));
        }
    }
}