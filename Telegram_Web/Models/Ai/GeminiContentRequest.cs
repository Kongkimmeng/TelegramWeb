namespace Telegram_Web.Models.Ai
{
    public class GeminiContentRequest
    {
        public List<GeminiContent> contents { get; set; }
    }

    public class GeminiContent
    {
        public List<GeminiPart> parts { get; set; }
    }

    public class GeminiPart
    {
        public string text { get; set; }
    }
}
