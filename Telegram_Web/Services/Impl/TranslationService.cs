
namespace Telegram_Web.Services.Impl
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        // Inject HttpClient and IConfiguration to read the API key
        public TranslationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleTranslate:ApiKey"];
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "API Key not configured.";
            }

            var apiUrl = $"https://translation.googleapis.com/language/translate/v2?key={_apiKey}";

            // The request body for the Google Translate API
            var requestBody = new
            {
                q = text,
                target = targetLanguage
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GoogleTranslateResponse>();
                    // The API returns a list of translations. We'll take the first one.
                    return result?.Data?.Translations?.FirstOrDefault()?.TranslatedText ?? "Translation not found.";
                }
                else
                {
                    // Read the error for better debugging
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"An exception occurred: {ex.Message}";
            }
        }
    }
    public class GoogleTranslateResponse
    {
        public TranslationData? Data { get; set; }
    }

    public class TranslationData
    {
        public List<Translation>? Translations { get; set; }
    }

    public class Translation
    {
        public string? TranslatedText { get; set; }
    }
}
