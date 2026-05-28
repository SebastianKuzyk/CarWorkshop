using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("vehicles")]
    public class Vehicle
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("customer_id")]
        public int? CustomerId { get; set; }

        [Column("make")]
        public string? Make { get; set; }

        [Column("model")]
        public string? Model { get; set; }

        [Column("year")]
        public int? Year { get; set; }

        [Column("license_plate")]
        public string? LicensePlate { get; set; }

        [Column("vin")]
        public string? Vin { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<RepairOrder> RepairOrders { get; set; } = new List<RepairOrder>();
    }
}
