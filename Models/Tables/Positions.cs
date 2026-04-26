using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Позиции")]
    public class Positions
    {
        [Key]
        [Column("ид_позиции")]
        public int ид_позиции { get; set; }

        [Column("ид_документа")]
        [Required]
        public int ид_документа { get; set; }  // ← исправлено: int, не string

        [Column("ид_товара")]
        [Required]
        public int ид_товара { get; set; }     // ← исправлено: int, не string

        [Column("количество")]
        [Required]
        public double количество { get; set; }

        [Column("цена_за_единицу")]
        [Required]
        public decimal цена_за_единицу { get; set; }

        [Column("скидка")]
        public decimal? скидка { get; set; }    // ← nullable

        // Новые поля для ТТН
        [Column("сумма_ндс")]
        public decimal? сумма_ндс { get; set; }

        [Column("стоимость_с_ндс")]
        public decimal? стоимость_с_ндс { get; set; }

        [Column("грузовых_мест")]
        public int? грузовых_мест { get; set; }

        [Column("масса_груза")]
        public decimal? масса_груза { get; set; }

        [Column("ставка_ндс")]
        public decimal? ставка_ндс { get; set; }

        [Column("примечание")]
        [MaxLength(500)]
        public string примечание { get; set; }

        // Навигационные свойства
        [ForeignKey("ид_документа")]
        public virtual Documents Документ { get; set; }

        [ForeignKey("ид_товара")]
        public virtual Goods Товар { get; set; }
    }
}