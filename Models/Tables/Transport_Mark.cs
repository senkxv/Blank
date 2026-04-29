using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("марки_транспорта")]
    public class Transport_Mark
    {
        [Key]
        [Column("ид_марки")]
        [Required]
        public int ид_марки { get; set; }

        [Column("наименование_марки")]
        [Required]
        [MaxLength(45)]
        public string? наименование_марки { get; set; }
    }
}