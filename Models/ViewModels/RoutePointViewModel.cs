namespace Blank.Models.ViewModels
{
    public class RoutePointViewModel
    {
        public int? ид_грузоотправителя { get; set; }
        public int? ид_пункта_погрузки { get; set; }
        public int? ид_пункта_разгрузки { get; set; }
        public int? ид_грузополучателя { get; set; }
        public string тип_точки { get; set; } = "погрузка";
    }
}