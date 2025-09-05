namespace Telegram_Web.Models
{
    public class MoreInfoTag
    {
        public int TagID { get; set; }
        public long ChatID { get; set; }
        public int BookID { get; set; }
        public string FieldName { get; set; }
        public string? FieldValue { get; set; }
        public DateTime CreateDate { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
