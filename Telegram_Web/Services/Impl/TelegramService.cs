using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Telegram_Web.Models;
using Telegram_Web.Models.Telegram;
using Telegram_Web.Pages;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Telegram_Web.Services.Impl
{
    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _http;

        public TelegramService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<TelegramChatStatus>> GetChatStatusListAsync(string empId)
        {
            string url = $"api/telegram/listchat?empId={empId}";
            return await _http.GetFromJsonAsync<List<TelegramChatStatus>>(url) 
                   ?? new List<TelegramChatStatus>();
        }


        public async Task<string> GetSummaryAsync(string fromDate, string toDate, long chatId)
        {  
            string formattedFromDate = DateTime.TryParse(fromDate, out var fd) ? fd.ToString("dd-MMM-yyyy") : fromDate;
            string formattedToDate = DateTime.TryParse(toDate, out var td) ? td.ToString("dd-MMM-yyyy") : toDate;

             var url = $"api/telegram/GetSummary?sdate={formattedFromDate}&edate={formattedToDate}&chatid={chatId}";
            return await _http.GetStringAsync(url);
        }

        public async Task<List<TelegramEmp>> GetTelegramEmp()
        {
             string url = $"api/telegram/telegram_emp";
             return await _http.GetFromJsonAsync<List<TelegramEmp>>(url) ?? new List<TelegramEmp>();
        }

        public async Task<List<TelegramEmp>> GetTelegramEmpAssign(long chatid)
        {
             string url = $"api/telegram/telegram_emp_by_chatid?chatid={chatid}";
             return await _http.GetFromJsonAsync<List<TelegramEmp>>(url) ?? new List<TelegramEmp>();
        }

        public async Task<List<TelegramChatStatus>> GetTelegramGroups(DateTime? fromDate, DateTime? toDate, string? groupType, string? title)
        {
                string url = "api/telegram/GetGroups?";

                if (fromDate != null)
                    url += $"fromDate={fromDate:yyyy-MM-dd}&";
                if (toDate != null)
                    url += $"toDate={toDate:yyyy-MM-dd}&";
                if (!string.IsNullOrEmpty(groupType))
                    url += $"groupType={Uri.EscapeDataString(groupType)}&";
                if (!string.IsNullOrEmpty(title))
                    url += $"title={Uri.EscapeDataString(title)}&";

                url = url.TrimEnd('&', '?');

                return await _http.GetFromJsonAsync<List<TelegramChatStatus>>(url) ?? new List<TelegramChatStatus>();
        }

        public async Task<List<TelegramMessage>> GetTelegramMessages(DateTime? fromDate, DateTime? toDate, long? chatid)
        {
            string url = "api/telegram/GetMessages?";

                if (fromDate != null)
                    url += $"fromDate={fromDate:yyyy-MM-dd}&";
                if (toDate != null)
                    url += $"toDate={toDate:yyyy-MM-dd}&";               
                if (chatid.HasValue)
                    url += $"chatId={chatid.Value}&";



                url = url.TrimEnd('&', '?');
                return await _http.GetFromJsonAsync<List<TelegramMessage>>(url) ?? new List<TelegramMessage>();
        }

        public async Task PostMarkAsReadAsync(long chatId, string empId)
        {
                var url = $"api/telegram/markread?empid={empId}&chatid={chatId}";
                await _http.PostAsync(url, null);
        }

        public async Task<HttpResponseMessage> PostSendMessageAsync(TelegramMessage message)
        {
              return await _http.PostAsJsonAsync("api/telegram/send", message);
        }

        public async Task Post_AssignTelegramEmpAsync(long chatId, string empId, bool assign)
        {
            var url = $"api/telegram/telegram_emp_assign?chatid={chatId}&empid={empId}&assign={assign}";
            var response = await _http.PostAsJsonAsync<object>(url, new { });
            response.EnsureSuccessStatusCode();
        }

    }
}
