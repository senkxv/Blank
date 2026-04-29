namespace Blank.Models.Views
{
    public class MainPage
    {
        public int ид_документа { get; set; }
        public string? тип { get; set; }
        public string? номер_документа { get; set; }
        public DateTime дата_создания { get; set; }
        public string? грузоотправитель { get; set; }
        public string? перевозчик { get; set; }
        public string? грузополучатель { get; set; }
        public string? пункт_погрузки { get; set; }
        public string? пункт_разгрузки { get; set; }
        public string? ФИО_Водителя { get; set; }
        public string? Марка_Машины { get; set; }
        public string? Регистрационный_Номер { get; set; }
        public string? Тип_ТС { get; set; }
    }
}