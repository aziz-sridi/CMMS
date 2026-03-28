namespace MaintenanceSystem.Models
{
    public enum LocationType
    {
        Building,
        Area,
        Line,
        Row
    }

    public class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public LocationType Type { get; set; }

        public Guid? ParentLocationId { get; set; }
        public Location? ParentLocation { get; set; }

        public List<Equipment> Equipments { get; set; } = new();
    }
}