using Blank.Models.Tables;

namespace Blank.Models.ViewModels
{
    public class DocumentPreviewViewModel
    {
        public Documents Документ { get; set; }
        public List<PositionViewModel> Позиции { get; set; }
        public Organization Грузоотправитель { get; set; }
        public Organization Грузополучатель { get; set; }
        public Organization Перевозчик { get; set; }
        public Drivers Водитель { get; set; }
        public Transport Транспорт { get; set; }
        public Loading_Point ПунктПогрузки { get; set; }
        public Unloading_Point ПунктРазгрузки { get; set; }
        public Document_Type ТипДокумента { get; set; }
        public DocumentTotals Итоги { get; set; }
        public string? ОснованиеОтпуска { get; set; }
        public string? НомерПломбы { get; set; }
        public string? ДоверенностьНомер { get; set; }
        public string? ДоверенностьДата { get; set; }
        public decimal РасстояниеПеревозки { get; set; }
        public decimal ОсновнойТариф { get; set; }
        public decimal КОплате { get; set; }
        public string? отпуск_разрешил { get; set; }
        public string? сдал_грузоотправитель { get; set; }
    }

    public class DocumentTotals
    {
        public decimal ВсегоКоличество { get; set; }
        public decimal ВсегоСтоимость { get; set; }
        public decimal ВсегоСуммаНДС { get; set; }
        public decimal ВсегоСтоимостьСНДС { get; set; }
        public decimal ВсегоМасса { get; set; }
        public int ВсегоМест { get; set; }
    }
}