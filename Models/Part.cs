using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("parts")]
    public class Part
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("price", TypeName = "numeric")]
        public decimal? Price { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        public virtual ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
        public virtual ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    }
}
