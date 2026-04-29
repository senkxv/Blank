using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Пользователи")]
    public class Users
    {
        [Key]
        [Column("ид_пользователя")]
        public int ид_пользователя { get; set; }

        [Column("почта")]
        [Required]
        [MaxLength(150)]
        public string? почта { get; set; }

        [Column("хэш_пароль")]
        [Required]
        [MaxLength(255)]
        public string? хэш_пароль { get; set; }

        [Column("фамилия")]
        [MaxLength(100)]
        public string? фамилия { get; set; }

        [Column("имя")]
        [MaxLength(100)]
        public string? имя { get; set; }

        [Column("отчество")]
        [MaxLength(100)]
        public string? отчество { get; set; }

        [Column("ид_должности")]
        public int? ид_должности { get; set; }

        [Column("активность")]
        [MaxLength(1)]
        public bool активность { get; set; }

        [Column("ид_организации")]
        public int? ид_организации { get; set; }
    }
}