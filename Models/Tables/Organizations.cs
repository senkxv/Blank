using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Организации")]
    public class Organization
    {
        [Key]
        [Column("ид_организации")]
        public int ид_организации { get; set; }

        [Column("название")]
        [Required]
        [MaxLength(255)]
        public string? название { get; set; }

        [Column("УНП")]
        [Required]
        [MaxLength(200)]
        public string? унп { get; set; }

        [Column("Адрес")]
        [Required]
        [MaxLength(150)]
        public string? адрес { get; set; }

        [Column("Почта")]
        [MaxLength(150)]
        public string? почта { get; set; }
    }
}