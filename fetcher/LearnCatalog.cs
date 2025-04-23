using System.Net.Http.Json;
namespace fetcher
{
    public class CatalogResponse
    {
        required public List<Module> Modules { get; set; }
    }

    public class LearnCatalog
    {
        private readonly HttpClient _httpClient;
        private readonly string _catalogUrl;

        public LearnCatalog(HttpClient httpClient, string catalogUrl)
        {
            _httpClient = httpClient;
            _catalogUrl = catalogUrl;
        }

        public async Task<List<Module>> GetModulesAsync()
        {
            var catalog = await _httpClient.GetFromJsonAsync<CatalogResponse>(_catalogUrl);
            return catalog?.Modules ?? new List<Module>();
        }
    }
}