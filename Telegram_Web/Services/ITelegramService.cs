using Telegram_Web.Models.Telegram;
using static System.Net.WebRequestMethods;

namespace Telegram_Web.Services
{
    public interface ITelegramService
    {
        Task<List<TelegramChatStatus>> GetTelegramGroups(DateTime? fromDate, DateTime? toDate, string? groupType, string? title);

        Task<List<TelegramMessage>> GetTelegramMessages(DateTime? fromDate, DateTime? toDate, long? chatid);
        
        Task<string> GetSummaryAsync(string fromDate, string toDate, long chatId);

        Task<List<TelegramChatStatus>> GetChatStatusListAsync(string empId);

        Task<List<TelegramEmp>> GetTelegramEmp();

        Task<List<TelegramEmp>> GetTelegramEmpAssign(long chatid);






        Task PostMarkAsReadAsync(long chatId, string empId);  

        Task<HttpResponseMessage> PostSendMessageAsync(TelegramMessage message);

        Task Post_AssignTelegramEmpAsync(long chatId, string empId, bool assign);  

      

    }
}
