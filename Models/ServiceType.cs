using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWorkshopWPF.Models
{
    [Table("service_types")]
    public class ServiceType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("default_price", TypeName = "numeric")]
        public decimal? DefaultPrice { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();
    }
}
