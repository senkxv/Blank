using System.ComponentModel.DataAnnotations.Schema;

namespace Blank.Models.Views
{
    [Table("Главная")]
    public class MainPage
    {
        public int ид_документа { get; set; }
        public string тип { get; set; }
        public string номер_документа { get; set; }
        public DateTime дата_создания { get; set; }
        public string грузоотправитель { get; set; }
        public string грузополучатель { get; set; }
        public int ид_пользователя { get; set; }
    }
}