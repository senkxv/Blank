using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Должности")]
    public class Posts
    {
        [Key]
        [Column("ид_должности")]
        [Required]
        public int ид_должности { get; set; }

        [Column("наименование")]
        [MaxLength(100)]
        public string наименование { get; set; }
    }
}