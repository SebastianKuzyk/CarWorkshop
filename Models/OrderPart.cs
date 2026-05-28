using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("order_parts")]
    public class OrderPart
    {
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("part_id")]
        public int PartId { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual RepairOrder RepairOrder { get; set; } = null!;

        [ForeignKey(nameof(PartId))]
        public virtual Part Part { get; set; } = null!;
    }
}
