using Telegram_Web.Models.Ai;

namespace Telegram_Web.Services
{
    public interface IAiService
    {
        Task<string> GenerateContentAsync(GeminiContentRequest request);
        Task<string> GenerateFullContentAsync(GeminiContentRequest request);
    }
}
