using System;
using System.Collections.Generic;

namespace MyApp.Models
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
        public EquipmentStatus Status { get; set; }

        public Guid LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public List<Failure> Failures { get; set; } = new();
    }
}