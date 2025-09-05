namespace Telegram_Web.Models.Telegram
{
    public class TelegramMessage
    {
        public int ID { get; set; }

        public long ChatID { get; set; }         // bigint → long

        public int MessageId { get; set; }       // int

        public string? Title { get; set; }       // nvarchar(50)

        public string? FirstName { get; set; }   // nvarchar(50)

        public string? LastName { get; set; }    // nvarchar(50)

        public string? Username { get; set; }    // nvarchar(50)

        public DateTime? Datetime { get; set; }  // datetime (nullable)

        public string? Text { get; set; }        // nvarchar(max)

        public string? Raw { get; set; }         // nvarchar(max)

        public long? FromUserID { get; set; }    // bigint (nullable)

        public string? EmpId { get; set; }       // varchar(10)

        public string? Type { get; set; } 

        public string? TypeCustom { get; set; }

        public int ReplyMessageId { get; set; }
    }


}
