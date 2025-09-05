namespace Telegram_Web.Models
{
    public class TelegramEmpAssign
    { 
        public int AssignmentID { get; set; }
        public long ChatID { get; set; }
        public string? EmpID { get; set; }      // Employee ID from table
        public string? Name { get; set; }       // Employee name from TAB_employee
        public DateTime AssignedAt { get; set; }
        public DateTime? UnassignedAt { get; set; }
        public bool IsActive { get; set; }
        public string? FromUserID { get; set; } 
    }
}
