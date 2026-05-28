using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("repair_tasks")]
    public class RepairTask
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("repair_order_id")]
        public int? RepairOrderId { get; set; }

        [Column("service_type_id")]
        public int? ServiceTypeId { get; set; }

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("price", TypeName = "numeric")]
        public decimal? Price { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; }

        [Column("needs_parts")]
        public bool NeedsParts { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(RepairOrderId))]
        public virtual RepairOrder? RepairOrder { get; set; }

        [ForeignKey(nameof(ServiceTypeId))]
        public virtual ServiceType? ServiceType { get; set; }

        public virtual ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    }
}
