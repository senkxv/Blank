using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models
{
    [Table("Позиции")]
    public class Positions
    {
        [Key]
        [Column("ид_позиции")]
        [Required]
        public int ид_позиции { get; set; }

        [Column("ид_документа")]
        [Required]
        [MaxLength(45)]
        public string ид_документа { get; set; }

        [Column("ид_товара")]
        [Required]
        [MaxLength(45)]
        public string ид_товара { get; set; }

        [Column("количество")]
        [Required]
        public double количество { get; set; }

        [Column("цена_за_единицу")]
        [Required]
        public decimal цена_за_единицу { get; set; }

        [Column("скидка")]
        [Required]
        public decimal скидка { get; set; }
    }
}