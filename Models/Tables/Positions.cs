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
        public int ид_документа { get; set; }

        [Column("ид_товара")]
        public int ид_товара { get; set; }

        [Column("количество")]
        public double количество { get; set; } 

        [Column("цена_за_единицу")]
        public decimal цена_за_единицу { get; set; }

        [Column("ставка_ндс")]
        public decimal? ставка_ндс { get; set; }

        [Column("скидка")]
        public decimal? скидка { get; set; }

        [Column("сумма_ндс")]
        public decimal? сумма_ндс { get; set; }

        [Column("стоимость_с_ндс")]
        public decimal? стоимость_с_ндс { get; set; }

        [Column("грузовых_мест")]
        public int? грузовых_мест { get; set; }

        [Column("масса_груза")]
        public decimal? масса_груза { get; set; }

        [Column("примечание")]
        public string? примечание { get; set; }

        [ForeignKey("ид_товара")]
        public virtual Goods? Товар { get; set; }
    }
}