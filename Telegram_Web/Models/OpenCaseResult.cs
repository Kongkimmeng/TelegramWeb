namespace Telegram_Web.Models
{
    public class OpenCaseResult
    {
        public string EmpID { get; set; }           
        public string Name { get; set; }
        public string TeamName { get; set; }
        public int OpenCaseCount { get; set; }
        public string OpenCaseIDs { get; set; }
        public string ChatIDs { get; set; }
        public string ChatTitles { get; set; } // semicolon-separated
    }
}
