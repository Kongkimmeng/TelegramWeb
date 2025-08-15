namespace Telegram_Web.Models.Telegram
{
    public class TelegramChatStatus
    {
        public string EmpID { get; set; } = string.Empty;
        public long ChatID { get; set; }
        public string? Title { get; set; }
        public DateTime? ReceivedTime { get; set; }
        public DateTime? LastReadTime { get; set; }
        public bool? IsRead { get; set; }
         public bool IsHover { get; set; } = false; 
    }
}
