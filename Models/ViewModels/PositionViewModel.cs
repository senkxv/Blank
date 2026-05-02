using System.ComponentModel.DataAnnotations;

namespace Blank.Models.ViewModels
{
    public class PositionViewModel
    {
        public int id { get; set; }

        [Required(ErrorMessage = "Выберите товар")]
        public int goodsId { get; set; }

        [Required(ErrorMessage = "Введите количество")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
        public double quantity { get; set; }

        [Required(ErrorMessage = "Введите цену")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal price { get; set; }

        [Range(0, 100, ErrorMessage = "Скидка должна быть от 0 до 100%")]
        public decimal discount { get; set; }

        [Range(0, 100, ErrorMessage = "Ставка НДС должна быть от 0 до 100%")]
        public decimal vatRate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Масса не может быть отрицательной")]
        public decimal weight { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество мест не может быть отрицательным")]
        public int packages { get; set; }
       
        [MaxLength(500, ErrorMessage = "Примечание не более 500 символов")]
        public string? note { get; set; }

        // Добавлено для предпросмотра
        public decimal? ставка_ндс { get; set; }
        public string? товар_наименование { get; set; }
        public string? единицы_измерения { get; set; }
    }
}