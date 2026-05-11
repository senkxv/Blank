using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Tables
{
    [Table("Точки_Маршрута")]
    public class RoutePoint
    {
        [Key]
        [Column("ид_точки")]
        public int ид_точки { get; set; }

        [Column("ид_маршрута")]
        [Required]
        public int ид_маршрута { get; set; }

        [Column("порядковый_номер")]
        [Required]
        public int порядковый_номер { get; set; }

        [Column("ид_пункта_погрузки")]
        public int? ид_пункта_погрузки { get; set; }

        [Column("ид_пункта_разгрузки")]
        public int? ид_пункта_разгрузки { get; set; }

        [Column("тип_точки")]
        [MaxLength(50)]
        public string тип_точки { get; set; } = "погрузка"; // погрузка, разгрузка, погрузка_разгрузка

        [Column("ид_грузоотправителя")]
        public int? ид_грузоотправителя { get; set; }

        [Column("ид_грузополучателя")]
        public int? ид_грузополучателя { get; set; }

        [ForeignKey("ид_грузоотправителя")]
        public virtual Organization Грузоотправитель { get; set; }

        [ForeignKey("ид_грузополучателя")]
        public virtual Organization Грузополучатель { get; set; }

        // Навигационные свойства
        [ForeignKey("ид_маршрута")]
        public virtual DeliveryRoute Маршрут { get; set; }

        [ForeignKey("ид_пункта_погрузки")]
        public virtual Loading_Point ПунктПогрузки { get; set; }

        [ForeignKey("ид_пункта_разгрузки")]
        public virtual Unloading_Point ПунктРазгрузки { get; set; }
    }
}