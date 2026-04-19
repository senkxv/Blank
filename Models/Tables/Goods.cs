using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Товары")]
    public class Goods
    {
        [Key]
        [Column("ид_товара")]
        [Required]
        public int ид_товара { get; set; }

        [Column("код_товара")]
        [Required]
        [MaxLength(45)]
        public string код_товара { get; set; }

        [Column("наименование")]
        [Required]
        [MaxLength(255)]
        public string наименование { get; set; }

        [Column("единицы_измерения")]
        [Required]
        [MaxLength(45)]
        public string единицы_измерения { get; set; }
    }
}