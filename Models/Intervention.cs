namespace CMMS.Domain.Entities;

public class Intervention
{
    public Guid Id { get; set; }
    public Guid FailureId { get; set; }
    public Guid TechnicianId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double Cost { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Failure Failure { get; set; } = null!;
    public Technician Technician { get; set; } = null!;
}
