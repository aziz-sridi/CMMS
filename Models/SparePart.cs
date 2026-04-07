using System;
using System.Collections.Generic;

namespace MyApp.Models
{
    public class SparePart
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public Guid? EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
    }
}
