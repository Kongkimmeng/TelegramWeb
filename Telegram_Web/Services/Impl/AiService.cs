using System.Text.Json;
using System.Text;
using Telegram_Web.Models.Ai;

namespace Telegram_Web.Services.Impl
{
    public class AiService : IAiService
    {
        private readonly HttpClient _http;
        public AiService(HttpClient http)
        {

            _http = http;
        }
        public async Task<string> GenerateFullContentAsync(GeminiContentRequest request)
        {
            var url = "api/gemini/generate"; // assuming your API is /api/gemini/generate
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API error: {response.StatusCode}, {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        public async Task<string> GenerateContentAsync(GeminiContentRequest request)
        {
            var url = "api/gemini/generate"; // your API endpoint
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API error: {response.StatusCode}, {error}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();

            // Deserialize to your response model
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<GeminiContentResponse>(resultJson, options);

            // Safely extract the text or return empty string
            return result?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text ?? string.Empty;
        }

    }
}
