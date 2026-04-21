using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("транспорт")]
    public class Transport
    {
        [Key]
        [Column("ид_транспорта")]
        [Required]
        public int ид_транспорта { get; set; }

        [Column("регистрационный_номер")]
        [Required]
        [MaxLength(100)]
        public string регистрационный_номер { get; set; }

        [Column("ид_типа_транспорта")]
        [Required]
        public int ид_типа_транспорта { get; set; }

        [Column("ид_марки")]
        [Required]
        public int ид_марки { get; set; }

        [ForeignKey("ид_типа_транспорта")]
        public virtual Transport_Type Тип_Транспорта { get; set; }

        [ForeignKey("ид_марки")]
        public virtual Transport_Mark Марка_Транспорта { get; set; }
    }
}