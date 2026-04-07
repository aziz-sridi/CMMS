namespace CMMS.Models;

public class Technician
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();

    // Notifications for the technician
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}