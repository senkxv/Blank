using Blank.Models.Tables;

namespace Blank.Models.ViewModels
{
    public class RouteDocumentViewModel
    {
        public int ид_маршрута { get; set; }
        public string название_маршрута { get; set; }
        public int текущая_точка_индекс { get; set; }
        public int всего_точек { get; set; }
        public RoutePoint текущая_точка { get; set; }
        public List<RoutePoint> все_точки { get; set; }

        // Предзаполненные данные
        public int ид_водителя { get; set; }
        public int ид_транспорта { get; set; }
        public int ид_перевозчика { get; set; }
        public int ид_грузоотправителя { get; set; }

        // Для выпадающих списков
        public List<Goods> Товары { get; set; }
        public List<Loading_Point> ПунктыПогрузки { get; set; }
        public List<Unloading_Point> ПунктыРазгрузки { get; set; }

        // Для формы документа
        public string номер_документа { get; set; }
        public DateTime дата_создания { get; set; } = DateTime.Now;
        public int ид_типа { get; set; }
        public List<Document_Type> ТипыДокументов { get; set; }
    }
}