namespace CMMS.Domain.Entities
{
    public class Technician
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<Intervention> Interventions { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }
}
