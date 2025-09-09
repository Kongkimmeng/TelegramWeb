namespace Telegram_Web.Models
{
    public class TelegramChatCase
    {
        public int CaseID { get; set; }
        public int MessageID { get; set; }
        public string MessageText { get; set; }
        public long ChatID { get; set; }
        public string CaseBy { get; set; }
        public DateTime CaseCreateDate { get; set; }
        public DateTime ActionTime { get; set; }
        public string Action { get; set; } 

        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; } 
        public DateTime UnassignedAt { get; set; }
        public string? UnassignedBy { get; set; } 
    }
}
