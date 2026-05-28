using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("part_requests")]
    public class PartRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("mechanic_id")]
        public int? MechanicId { get; set; }

        [Column("repair_task_id")]
        public int? RepairTaskId { get; set; }

        [Column("part_id")]
        public int? PartId { get; set; }

        [Column("custom_part_name")]
        public string? CustomPartName { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [Column("status")]
        public string? Status { get; set; } = "Brak odpowiedzi";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(MechanicId))]
        public virtual User? Mechanic { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        [ForeignKey(nameof(RepairTaskId))]
        public virtual RepairTask? RepairTask { get; set; }
    }
}
