using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Водители")]
    public class Drivers
    {
        [Key]
        [Column("ид_водителя")]
        public int Id { get; set; }

        [Column("фамилия")]
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Column("имя")]
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Column("отчество")]
        [Required]
        [MaxLength(100)]
        public string Patronymic { get; set; }

        [Column("номер_лицензии")]
        [Required]
        [MaxLength(255)]
        public string LicenseNumber { get; set; }

        [Column("номер_телефона")]
        [Required]
        [MaxLength(45)]
        public string PhoneNumber { get; set; }
    }
}