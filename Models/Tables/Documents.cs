using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Документы")]
    public class Documents
    {
        [Key]
        [Column("ид_документа")]
        public int ид_документа { get; set; }

        [Column("ид_типа")]
        [Required]
        public int ид_типа { get; set; }

        [Column("номер_документа")]
        [Required]
        [MaxLength(255)]
        public string номер_документа { get; set; }

        [Column("дата_создания")]
        [Required]
        public DateTime дата_создания { get; set; }

        [Column("ид_перевозчика")]
        [Required]
        public int ид_перевозчика { get; set; }

        [Column("ид_грузоотправителя")]
        [Required]
        public int ид_грузоотправителя { get; set; }

        [Column("ид_получателя")]
        [Required]
        public int ид_получателя { get; set; }

        [Column("ид_водителя")]
        [Required]
        public int ид_водителя { get; set; }

        [Column("ид_транспорта")]
        [Required]
        public int ид_транспорта { get; set; }

        [Column("ид_пользователя")]
        [Required]
        public int ид_пользователя { get; set; }

        [Column("ид_пункта_погрузки")]
        [Required]
        public int ид_пункта_погрузки { get; set; }

        [Column("ид_пункта_разгрузки")]
        [Required]
        public int ид_пункта_разгрузки { get; set; }
    }
}