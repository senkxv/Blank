using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("пункт_разгрузки")]
    public class Unloading_Point
    {
        [Key]
        [Column("ид_пункта_разгрузки")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ид_пункта_разгрузки { get; set; }

        [Column("наименование")]
        [Required]
        [MaxLength(255)]
        public string наименование { get; set; }
    }
}