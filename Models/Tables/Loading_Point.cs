using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("пункт_погрузки")]
    public class Loading_Point
    {
        [Key]
        [Column("ид_пункта_погрузки")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdПунктаПогрузки { get; set; }

        [Column("наименование")]
        [Required]
        [MaxLength(255)]
        public string наименование { get; set; }
    }
}