using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Водители")]
    public class Drivers
    {
        [Key]
        [Column("ид_водителя")]
        public int ид_водителя { get; set; }

        [Column("фамилия")]
        [Required]
        [MaxLength(100)]
        public string фамилия { get; set; }

        [Column("имя")]
        [Required]
        [MaxLength(100)]
        public string имя { get; set; }

        [Column("отчество")]
        [Required]
        [MaxLength(100)]
        public string отчество { get; set; }

        [Column("номер_лицензии")]
        [Required]
        [MaxLength(255)]
        public string номер_лицензии { get; set; }

        [Column("номер_телефона")]
        [Required]
        [MaxLength(45)]
        public string номер_телефона { get; set; }
    }
}