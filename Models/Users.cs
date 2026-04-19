using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models
{
    [Table("Пользователи")]
    public class Users
    {
        [Key]
        [Column("ид_пользователя")]
        [Required]
        public int ид_пользователя { get; set; }

        [Column("почта")]
        [Required]
        [MaxLength(150)]
        public string почта { get; set; }

        [Column("ххш_пароль")]
        [Required]
        [MaxLength(255)]
        public string хэш_пароль { get; set; }

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

        [Column("ид_должности")]
        [Required]
        public int ид_должности { get; set; }

        [Column("активность")]
        [Required]
        [MaxLength(1)]
        public string активность { get; set; }

        [Column("ид_организации")]
        [Required]
        public int ид_организации { get; set; }
    }
}