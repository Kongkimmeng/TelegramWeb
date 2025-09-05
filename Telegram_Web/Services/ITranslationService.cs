using System.Net.Http;

namespace Telegram_Web.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLanguage);
    }
}
