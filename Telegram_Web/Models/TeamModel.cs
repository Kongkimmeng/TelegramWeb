namespace Telegram_Web.Models
{
    public class TeamModel
    { 
        public int TeamID { get; set; }       // int
        public string? TeamName { get; set; }       // nvarchar(50)\
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
    }
}
