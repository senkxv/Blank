using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("типы_документов")]
    public class Document_Type
    {
        [Key]
        [Column("ид_типа")]
        public int ид_типа { get; set; }

        [Column("краткое_наименование")]
        [MaxLength(100)]
        public string? краткое_наименование { get; set; }

        [Column("полное_наименование")]
        [MaxLength(100)]
        public string? полное_наименование { get; set; }
    }
}