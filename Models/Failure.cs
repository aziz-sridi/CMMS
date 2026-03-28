using System;

namespace MyApp.Models
{
    public enum FailureSeverity
    {
        Low,
        Medium,
        High
    }

    public enum FailureStatus
    {
        Open,
        InProgress,
        Closed
    }

    public class Failure
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public FailureSeverity Severity { get; set; }
        public FailureStatus Status { get; set; }

        public Guid EquipmentId { get; set; }
        public Equipment Equipment { get; set; } = null!;
    }
}