using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("repair_orders")]
    public class RepairOrder
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("vehicle_id")]
        public int? VehicleId { get; set; }

        [Column("mechanic_id")]
        public int? MechanicId { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Nierozpoczęte";

        [Column("description")]
        public string? Description { get; set; }

        [Column("service_price", TypeName = "numeric")]
        public decimal? ServicePrice { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey(nameof(MechanicId))]
        public virtual User? Mechanic { get; set; }

        public virtual ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
        public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();
    }
}
