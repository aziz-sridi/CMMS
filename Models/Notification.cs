using System;

namespace CMMS.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public Guid? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
    }
}
