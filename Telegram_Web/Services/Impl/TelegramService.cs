using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Telegram_Web.Models;
using Telegram_Web.Models.Ai;
using Telegram_Web.Models.Telegram;
using Telegram_Web.Pages;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Telegram_Web.Services.Impl
{
    public class TelegramService : ITelegramService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _botToken;

        public TelegramService(IHttpClientFactory httpClientFactory,  IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _botToken = configuration["TelegramSettings:BotToken"];  
        }



        //public async Task<bool> SendTextMessageAsync(string chatId, string text)
        //{
           
        //    // Use IHttpClientFactory to create client
        //    var client = _httpClientFactory.CreateClient();

        //    var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        //    var payload = new Dictionary<string, string>
        //    {
        //        { "chat_id", chatId },
        //        { "text", text }
        //    };

        //    using var content = new FormUrlEncodedContent(payload);
        //    var response = await client.PostAsync(url, content);

        //    return response.IsSuccessStatusCode;
        //}
        public async Task<int?> SendTextMessageAsync(string chatId, string text, int replyToMessageId = 0)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            // Prepare payload
            var payload = new Dictionary<string, string>
            {
                { "chat_id", chatId },
                { "text", text }
            };

            if (replyToMessageId > 0)
            {
                payload.Add("reply_to_message_id", replyToMessageId.ToString());
            }

            var content = new FormUrlEncodedContent(payload);
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return null; // or throw exception depending on your needs
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Telegram response looks like: { "ok":true, "result": { "message_id":123, ... } }
            if (root.TryGetProperty("result", out var result) &&
                result.TryGetProperty("message_id", out var messageIdElement))
            {
                return messageIdElement.GetInt32();
            }

            return null;
        }






        public async Task<bool> SendVoiceMessageAsync(string botToken, string chatId, byte[] audioData)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{botToken}/sendVoice";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");

            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/ogg");

            // Telegram API requires a filename for the voice file part.
            content.Add(audioContent, "voice", "voice.ogg");

            var response = await client.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendPhotoAsync(string botToken, string chatId, byte[] photoData, string fileName, string? caption = null)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{botToken}/sendPhoto";

            using var content = new MultipartFormDataContent();

            // Add required fields: chat_id and the photo file
            content.Add(new StringContent(chatId), "chat_id");
            var photoContent = new ByteArrayContent(photoData);
            photoContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(photoContent, "photo", fileName);

            // Add the optional caption if it exists
            if (!string.IsNullOrEmpty(caption))
            {
                content.Add(new StringContent(caption), "caption");
            }

            var response = await client.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendTelegramNotification(string telegramUserId, string text)
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            using var client = new HttpClient();
            var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "chat_id", telegramUserId },
                { "text", text }
            }));

            return response.IsSuccessStatusCode;
        }




        //public async Task PostMarkAsReadAsync(long chatId, string empId)
        //{
        //        var url = $"api/telegram/markread?empid={empId}&chatid={chatId}";
        //        await _http.PostAsync(url, null);
        //}

        //public async Task<HttpResponseMessage> PostSendMessageAsync(TelegramMessage message)
        //{
        //      return await _http.PostAsJsonAsync("api/telegram/send", message);
        //}









    }
}
