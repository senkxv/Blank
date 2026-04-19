using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("типы_транспорта")]
    public class Transport_Type
    {
        [Key]
        [Column("ид_типа_транспорта")]
        [Required]
        public int ид_типа_транспорта { get; set; }

        [Column("наименование_типа")]
        [Required]
        [MaxLength(100)]
        public string наименование_типа { get; set; }
    }
}