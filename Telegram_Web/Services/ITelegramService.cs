using Telegram_Web.Models.Telegram;
using static System.Net.WebRequestMethods;

namespace Telegram_Web.Services
{
    public interface ITelegramService
    {

        //Task<bool> SendTextMessageAsync(string chatId, string text);
        Task<int?> SendTextMessageAsync(string chatId, string text, int replyToMessageId = 0);
        Task<bool> SendVoiceMessageAsync(string botToken, string chatId, byte[] audioData);
        Task<bool> SendPhotoAsync(string botToken, string chatId, byte[] photoData, string fileName, string? caption = null);        
        Task<bool> SendTelegramNotification(string telegramUserId, string text);




        //Task PostMarkAsReadAsync(long chatId, string empId);  

        //Task<HttpResponseMessage> PostSendMessageAsync(TelegramMessage message);
    }
}
