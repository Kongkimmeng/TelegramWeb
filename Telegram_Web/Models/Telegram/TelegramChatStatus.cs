namespace Telegram_Web.Models.Telegram
{
    public class TelegramChatStatus
    {
        public long ChatID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TelegramFirstname { get; set; } = string.Empty;
        public string TelegramLastname { get; set; } = string.Empty;
        public string TelegramUsername { get; set; } = string.Empty;
        public string LastMessageText { get; set; } = string.Empty;
        public DateTime? LastMessageTime { get; set; }
        public string StatusName { get; set; } = string.Empty;  // Open / Closed
        public DateTime CreateAt { get; set; }
        public DateTime? OpenAt { get; set; }
        public DateTime? CloseAt { get; set; }
        public bool IsRead { get; set; } = false; // for new/unread badge
        public bool IsAssign { get; set; }
    }

}
