using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("маршруты")]
    public class DeliveryRoute
    {
        [Key]
        [Column("ид_маршрута")]
        public int ид_маршрута { get; set; }

        [Column("название")]
        [Required]
        [MaxLength(255)]
        public string название { get; set; }

        [Column("ид_организации")]
        [Required]
        public int ид_организации { get; set; }

        [Column("ид_водителя")]
        public int? ид_водителя { get; set; }

        [Column("ид_транспорта")]
        public int? ид_транспорта { get; set; }

        [Column("ид_перевозчика")]
        public int? ид_перевозчика { get; set; }

        [Column("статус")]
        [MaxLength(50)]
        public string статус { get; set; } = "активен"; // активен, завершен

        // Навигационные свойства
        [ForeignKey("ид_организации")]
        public virtual Organization Организация { get; set; }

        [ForeignKey("ид_водителя")]
        public virtual Drivers Водитель { get; set; }

        [ForeignKey("ид_транспорта")]
        public virtual Transport Транспорт { get; set; }

        [ForeignKey("ид_перевозчика")]
        public virtual Organization Перевозчик { get; set; }

        public virtual ICollection<RoutePoint> ТочкиМаршрута { get; set; }

        [Column("ид_типа")]
        public int? ид_типа { get; set; } = 1; // 1 = ТТН по умолчанию

        [ForeignKey("ид_типа")]
        public virtual Document_Type ТипДокумента { get; set; }
    }
}