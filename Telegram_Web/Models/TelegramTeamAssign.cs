namespace Telegram_Web.Models
{
   public class TelegramTeamAssign
    {
        public int AssignmentID { get; set; }
        public long ChatID { get; set; }
        public int TeamID { get; set; }        // Team ID from table
        public string? TeamName { get; set; }   // Team name from TAB_Team
        public DateTime AssignedAt { get; set; }
        public DateTime? UnassignedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
