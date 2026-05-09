namespace CMMS.Domain.Entities
{
    public enum EquipmentStatus
    {
        Active,
        OutOfService,
        UnderMaintenance
    }

    public class Equipment
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int Criticality { get; set; } // 1-5
        public DateTime PurchaseDate { get; set; }
        public double? PurchaseCost { get; set; }
        public int? ExpectedLifetimeMonths { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public EquipmentStatus Status { get; set; }

        public Guid LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public List<Failure> Failures { get; set; } = new();
        public List<SparePart> SpareParts { get; set; } = new();
    }
}
