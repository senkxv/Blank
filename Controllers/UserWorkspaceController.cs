using Blank.Data;
using Blank.Helpers;
using Blank.Models.Tables;
using Blank.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SelectPdf;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace Blank.Controllers
{
    public class UserWorkspaceController : Controller
    {
        private readonly ApplicationDBContext _context;

        public UserWorkspaceController(ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult Error404()
        {
            return View();
        }

        public IActionResult Error500()
        {
            return View();
        }

        public IActionResult Index()
        {
            var данные = _context.Главная.ToList();
            return View(данные);
        }

        [HttpGet]
        public IActionResult CreateDocumentPage()
        {
            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();
            ViewBag.Goods = _context.Товары.ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDocumentPage(Documents document, string positionsData)
        {
            try
            {
                if (document.дата_создания == default)
                {
                    document.дата_создания = DateTime.Now;
                }

                _context.Документы.Add(document);
                await _context.SaveChangesAsync();

                int documentId = document.ид_документа;

                int positionsSavedCount = 0;

                if (!string.IsNullOrEmpty(positionsData))
                {
                    try
                    {
                        var positions = JsonSerializer.Deserialize<List<PositionViewModel>>(positionsData);

                        if (positions != null && positions.Any())
                        {
                            foreach (var pos in positions)
                            {
                                if (pos.goodsId <= 0 || pos.quantity <= 0 || pos.price <= 0)
                                {
                                    continue;
                                }

                                decimal quantityDecimal = (decimal)pos.quantity;
                                decimal cost = pos.price * quantityDecimal;
                                decimal vatAmount = cost * (pos.vatRate / 100);
                                decimal totalWithVat = cost + vatAmount;

                                var position = new Positions
                                {
                                    ид_документа = documentId,
                                    ид_товара = pos.goodsId,
                                    количество = pos.quantity,
                                    цена_за_единицу = pos.price,
                                    ставка_ндс = pos.vatRate > 0 ? pos.vatRate : (decimal?)null,
                                    скидка = pos.discount > 0 ? pos.discount : (decimal?)null,
                                    масса_груза = pos.weight > 0 ? pos.weight : (decimal?)null,
                                    грузовых_мест = pos.packages > 0 ? pos.packages : (int?)null,
                                    примечание = string.IsNullOrEmpty(pos.note) ? null : pos.note,
                                    сумма_ндс = vatAmount,
                                    стоимость_с_ндс = totalWithVat
                                };

                                _context.Позиции.Add(position);
                                positionsSavedCount++;
                            }

                            if (positionsSavedCount > 0)
                            {
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        ModelState.AddModelError("", "Ошибка при обработке данных товаров");
                    }
                }

                TempData["Success"] = $"Документ успешно создан. Добавлено позиций: {positionsSavedCount}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();
            ViewBag.Goods = _context.Товары.ToList();

            return View(document);
        }

        [HttpGet]
        public IActionResult EditDocumentPage(int id)
        {
            var document = _context.Документы.Find(id);
            if (document == null)
            {
                return NotFound();
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();
            ViewBag.Goods = _context.Товары.ToList();

            var existingPositions = _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == id)
                .AsEnumerable()
                .Select(p => new Positions
                {
                    ид_позиции = p.ид_позиции,
                    ид_документа = p.ид_документа,
                    ид_товара = p.ид_товара,
                    количество = p.количество,
                    цена_за_единицу = p.цена_за_единицу,
                    ставка_ндс = p.ставка_ндс ?? 0,
                    масса_груза = p.масса_груза ?? 0,
                    грузовых_мест = p.грузовых_мест ?? 0,
                    скидка = p.скидка ?? 0,
                    примечание = p.примечание ?? "",
                    сумма_ндс = p.сумма_ндс ?? 0,
                    стоимость_с_ндс = p.стоимость_с_ндс ?? 0,
                    Товар = p.Товар
                })
                .ToList();

            ViewBag.ExistingPositions = existingPositions;

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocumentPage(int id, Documents document, string positionsData, string deletedPositions)
        {
            if (id != document.ид_документа)
            {
                return NotFound();
            }

            try
            {
                _context.Update(document);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(deletedPositions))
                {
                    var deletedIds = deletedPositions.Split(',').Select(int.Parse).ToList();
                    var toDelete = _context.Позиции.Where(p => deletedIds.Contains(p.ид_позиции));
                    _context.Позиции.RemoveRange(toDelete);
                    await _context.SaveChangesAsync();
                }

                if (!string.IsNullOrEmpty(positionsData))
                {
                    var positions = JsonSerializer.Deserialize<List<PositionViewModel>>(positionsData);
                    if (positions != null)
                    {
                        int savedCount = 0;

                        foreach (var pos in positions)
                        {
                            if (pos.goodsId <= 0 || pos.quantity <= 0 || pos.price <= 0)
                            {
                                continue;
                            }

                            decimal quantityDecimal = (decimal)pos.quantity;
                            decimal cost = pos.price * quantityDecimal;
                            decimal vatAmount = cost * (pos.vatRate / 100);
                            decimal totalWithVat = cost + vatAmount;

                            if (pos.id > 0)
                            {
                                var existing = await _context.Позиции.FindAsync(pos.id);
                                if (existing != null)
                                {
                                    existing.ид_товара = pos.goodsId;
                                    existing.количество = pos.quantity;
                                    existing.цена_за_единицу = pos.price;
                                    existing.ставка_ндс = pos.vatRate > 0 ? pos.vatRate : (decimal?)null;
                                    existing.скидка = pos.discount > 0 ? pos.discount : (decimal?)null;
                                    existing.масса_груза = pos.weight > 0 ? pos.weight : (decimal?)null;
                                    existing.грузовых_мест = pos.packages > 0 ? pos.packages : (int?)null;
                                    existing.примечание = string.IsNullOrEmpty(pos.note) ? null : pos.note;
                                    existing.сумма_ндс = vatAmount;
                                    existing.стоимость_с_ндс = totalWithVat;
                                    savedCount++;
                                }
                            }
                            else
                            {
                                var newPos = new Positions
                                {
                                    ид_документа = document.ид_документа,
                                    ид_товара = pos.goodsId,
                                    количество = pos.quantity,
                                    цена_за_единицу = pos.price,
                                    ставка_ндс = pos.vatRate > 0 ? pos.vatRate : (decimal?)null,
                                    скидка = pos.discount > 0 ? pos.discount : (decimal?)null,
                                    масса_груза = pos.weight > 0 ? pos.weight : (decimal?)null,
                                    грузовых_мест = pos.packages > 0 ? pos.packages : (int?)null,
                                    примечание = string.IsNullOrEmpty(pos.note) ? null : pos.note,
                                    сумма_ндс = vatAmount,
                                    стоимость_с_ндс = totalWithVat
                                };
                                _context.Позиции.Add(newPos);
                                savedCount++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "Документ успешно обновлен";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();
            ViewBag.Goods = _context.Товары.ToList();

            return View(document);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var документ = await _context.Документы.FindAsync(id);
                if (документ == null)
                {
                    TempData["Error"] = $"Документ с ID {id} не найден";
                    return RedirectToAction("Index");
                }

                var позиции = await _context.Позиции.Where(p => p.ид_документа == id).ToListAsync();
                int позицииCount = позиции.Count;

                if (позиции.Any())
                {
                    _context.Позиции.RemoveRange(позиции);
                    await _context.SaveChangesAsync();
                }

                _context.Документы.Remove(документ);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Документ №{документ.номер_документа} успешно удален вместе с {позицииCount} позициями";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ExportAllToExcel()
        {
            using (var package = new ExcelPackage())
            {
                var sheetDocuments = package.Workbook.Worksheets.Add("Документы");

                sheetDocuments.Cells[1, 1].Value = "Порядковый номер";
                sheetDocuments.Cells[1, 2].Value = "Тип документа";
                sheetDocuments.Cells[1, 3].Value = "Номер документа";
                sheetDocuments.Cells[1, 4].Value = "Дата создания";
                sheetDocuments.Cells[1, 5].Value = "Грузоотправитель";
                sheetDocuments.Cells[1, 6].Value = "Перевозчик";
                sheetDocuments.Cells[1, 7].Value = "Грузополучатель";
                sheetDocuments.Cells[1, 8].Value = "Пункт погрузки";
                sheetDocuments.Cells[1, 9].Value = "Пункт разгрузки";
                sheetDocuments.Cells[1, 10].Value = "ФИО водителя";
                sheetDocuments.Cells[1, 11].Value = "Марка машины";
                sheetDocuments.Cells[1, 12].Value = "Регистрационный номер";
                sheetDocuments.Cells[1, 13].Value = "Тип ТС";

                var документы = _context.Главная.ToList();
                int row = 2;
                foreach (var doc in документы)
                {
                    sheetDocuments.Cells[row, 1].Value = doc.ид_документа;
                    sheetDocuments.Cells[row, 2].Value = doc.тип;
                    sheetDocuments.Cells[row, 3].Value = doc.номер_документа;
                    sheetDocuments.Cells[row, 4].Value = doc.дата_создания.ToString("yyyy-MM-dd");
                    sheetDocuments.Cells[row, 5].Value = doc.грузоотправитель;
                    sheetDocuments.Cells[row, 6].Value = doc.перевозчик;
                    sheetDocuments.Cells[row, 7].Value = doc.грузополучатель;
                    sheetDocuments.Cells[row, 8].Value = doc.пункт_погрузки;
                    sheetDocuments.Cells[row, 9].Value = doc.пункт_разгрузки;
                    sheetDocuments.Cells[row, 10].Value = doc.ФИО_Водителя;
                    sheetDocuments.Cells[row, 11].Value = doc.Марка_Машины;
                    sheetDocuments.Cells[row, 12].Value = doc.Регистрационный_Номер;
                    sheetDocuments.Cells[row, 13].Value = doc.Тип_ТС;
                    row++;
                }

                sheetDocuments.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Документы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchString)
        {
            var данные = await _context.Главная.ToListAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower().Trim();

                DateTime? searchDate = null;
                string[] dateFormats = {
                    "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy",
                    "dd.MM.yy", "dd/MM/yy", "dd-MM-yy",
                    "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
                    "MM/dd/yyyy", "MM-dd-yyyy", "MM.dd.yyyy"
                };

                if (DateTime.TryParseExact(searchString, dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    searchDate = parsedDate;
                }

                else if (DateTime.TryParse(searchString, out DateTime simpleDate))
                {
                    searchDate = simpleDate;
                }

                данные = данные.Where(d =>
                    d.ид_документа.ToString().Contains(searchString) ||
                    (d.тип != null && d.тип.ToLower().Contains(searchString)) ||
                    (d.номер_документа != null && d.номер_документа.ToLower().Contains(searchString)) ||
                    (d.грузоотправитель != null && d.грузоотправитель.ToLower().Contains(searchString)) ||
                    (d.перевозчик != null && d.перевозчик.ToLower().Contains(searchString)) ||
                    (d.грузополучатель != null && d.грузополучатель.ToLower().Contains(searchString)) ||
                    (d.пункт_погрузки != null && d.пункт_погрузки.ToLower().Contains(searchString)) ||
                    (d.пункт_разгрузки != null && d.пункт_разгрузки.ToLower().Contains(searchString)) ||
                    (d.ФИО_Водителя != null && d.ФИО_Водителя.ToLower().Contains(searchString)) ||
                    (d.Марка_Машины != null && d.Марка_Машины.ToLower().Contains(searchString)) ||
                    (d.Регистрационный_Номер != null && d.Регистрационный_Номер.ToLower().Contains(searchString)) ||
                    (d.Тип_ТС != null && d.Тип_ТС.ToLower().Contains(searchString)) ||

                    (searchDate.HasValue && d.дата_создания.Date == searchDate.Value.Date) ||

                    d.дата_создания.Year.ToString().Contains(searchString) ||

                    d.дата_создания.ToString("MM.yyyy").Contains(searchString.Replace("/", ".").Replace("-", ".")) ||
                    d.дата_создания.ToString("yyyy").Contains(searchString)
                ).ToList();
            }

            ViewBag.SearchString = searchString;
            return View("Index", данные);
        }

        [HttpGet]
        public async Task<IActionResult> PrintDocument(int id)
        {
            var документ = await _context.Документы
                .FirstOrDefaultAsync(d => d.ид_документа == id);

            if (документ == null)
            {
                return NotFound();
            }

            var позиции = await _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == id)
                .ToListAsync();

            var грузоотправитель = await _context.Организации
                .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
            var грузополучатель = await _context.Организации
                .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
            var перевозчик = await _context.Организации
                .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_перевозчика);
            var водитель = await _context.Водители
                .FirstOrDefaultAsync(d => d.ид_водителя == документ.ид_водителя);
            var транспорт = await _context.Транспорт
                .FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);
            var пунктПогрузки = await _context.Пункт_Погрузки
                .FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
            var пунктРазгрузки = await _context.Пункт_Разгрузки
                .FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);
            var типДокумента = await _context.Типы_Документов
                .FirstOrDefaultAsync(t => t.ид_типа == документ.ид_типа);

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "ttn.cshtml");
            if (!System.IO.File.Exists(templatePath))
            {
                return Content("Шаблон не найден: " + templatePath);
            }

            var htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            var goodsHtml = new StringBuilder();
            decimal totalQuantity = 0, totalCost = 0, totalVat = 0, totalWeight = 0;
            int totalPackages = 0;

            foreach (var pos in позиции)
            {
                decimal quantityDecimal = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * quantityDecimal;
                decimal vatAmount = cost * ((pos.ставка_ндс ?? 0) / 100);

                totalQuantity += quantityDecimal;
                totalCost += cost;
                totalVat += vatAmount;
                totalWeight += pos.масса_груза ?? 0;    
                totalPackages += pos.грузовых_мест ?? 0;

                goodsHtml.AppendLine($@"
                    <tr class=""goods-row"">
                        <td>{pos.Товар?.наименование ?? ""}</td>
                        <td class=""center"">{pos.Товар?.единицы_измерения ?? ""}</td>
                        <td class=""right"">{pos.количество:F3}</td>
                        <td class=""right"">{pos.цена_за_единицу:F2}</td>
                        <td class=""right"">{cost:F2}</td>
                        <td class=""center"">{pos.ставка_ндс ?? 0}</td>
                        <td class=""right"">{vatAmount:F2}</td>
                        <td class=""right"">{cost + vatAmount:F2}</td>
                        <td class=""right"">{pos.грузовых_мест ?? 0}</td>
                        <td class=""right"">{pos.масса_груза ?? 0:F3}</td>
                        <td class=""right"">{pos.примечание ?? ""}</td>
                    </tr>"
                );
            }

            var html = htmlTemplate
                .Replace("{{НомерДокумента}}", документ.номер_документа ?? "")
                .Replace("{{Тип}}", типДокумента?.краткое_наименование ?? "")
                .Replace("{{ДатаСоздания}}", документ.дата_создания.ToString("dd.MM.yyyy"))
                .Replace("{{Грузоотправитель}}", грузоотправитель?.название ?? "")
                .Replace("{{УНП_Грузоотправитель}}", грузоотправитель?.унп ?? "")
                .Replace("{{Адрес_Грузоотправитель}}", грузоотправитель?.адрес ?? "")
                .Replace("{{Грузополучатель}}", грузополучатель?.название ?? "")
                .Replace("{{УНП_Грузополучатель}}", грузополучатель?.унп ?? "")
                .Replace("{{Адрес_Грузополучатель}}", грузополучатель?.адрес ?? "")
                .Replace("{{Перевозчик}}", перевозчик?.название ?? "")
                .Replace("{{УНП_Перевозчик}}", перевозчик?.унп ?? "")
                .Replace("{{Адрес_Перевозчик}}", перевозчик?.адрес ?? "")

                .Replace("{{РегистрационныйНомер}}", транспорт?.регистрационный_номер ?? "")

                .Replace("{{ФИОВодителя}}", водитель != null ? $"{водитель.фамилия} {водитель.имя} {водитель.отчество}" : "")
                .Replace("{{Лицензия}}", водитель?.номер_лицензии ?? "")
                .Replace("{{ПутевойЛист}}", "")
                .Replace("{{ПунктПогрузки}}", пунктПогрузки?.наименование ?? "")
                .Replace("{{ПунктРазгрузки}}", пунктРазгрузки?.наименование ?? "")
                .Replace("{{АдресПогрузки}}", пунктПогрузки?.наименование ?? "")
                .Replace("{{АдресРазгрузки}}", пунктРазгрузки?.наименование ?? "")
                .Replace("{{Позиции}}", goodsHtml.ToString())
                .Replace("{{ВсегоКоличество}}", totalQuantity.ToString("F3"))
                .Replace("{{ВсегоСтоимость}}", totalCost.ToString("F2"))
                .Replace("{{ВсегоСуммаНДС}}", totalVat.ToString("F2"))
                .Replace("{{ВсегоСтоимостьСНДС}}", (totalCost + totalVat).ToString("F2"))
                .Replace("{{ВсегоМест}}", totalPackages.ToString())
                .Replace("{{ВсегоМасса}}", totalWeight.ToString("F3"))
                .Replace("{{ВсегоСуммаНДСПрописью}}", NumToTextHelper.SumInWords(totalVat))
                .Replace("{{ВсегоСтоимостьСНДСПрописью}}", NumToTextHelper.SumInWords(totalCost + totalVat))
                .Replace("{{ВсегоМассаПрописью}}", NumToTextHelper.WeightInWords(totalWeight))
                .Replace("{{ВсегоМестПрописью}}", NumToTextHelper.PackagesInWords(totalPackages))
                .Replace("{{ОтпускРазрешил}}", "")
                .Replace("{{ТоварПринял}}", "")
                .Replace("{{СдалГрузоотправитель}}", "")
                .Replace("{{НомерПломбы}}", "")
                .Replace("{{Доверенность}}", "")
                .Replace("{{ДатаДоверенности}}", "")
                .Replace("{{Расстояние}}", "")
                .Replace("{{ОсновнойТариф}}", "")
                .Replace("{{КОплате}}", "")
                .Replace("{{ВодительПодпись}}", "")
                .Replace("{{ПредставительПодпись}}", "");

            var converter = new HtmlToPdf();
            var pdf = converter.ConvertHtmlString(html);
            var pdfBytes = pdf.Save();

            return File(pdfBytes, "application/pdf", $"ТТН_{документ.номер_документа}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            try
            {
                var документ = await _context.Документы
                    .FirstOrDefaultAsync(d => d.ид_документа == id);

                if (документ == null)
                {
                    return Content($"Документ с ID {id} не найден", "text/html");
                }

                var позиции = new List<PositionViewModel>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                SELECT 
                    p.ид_позиции,
                    p.ид_товара,
                    IFNULL(p.количество, 0) as количество,
                    IFNULL(p.цена_за_единицу, 0) as цена_за_единицу,
                    p.ставка_ндс,
                    p.масса_груза,
                    p.грузовых_мест,
                    p.примечание,
                    IFNULL(g.наименование, 'Товар не найден') as товар_наименование,
                    IFNULL(g.единицы_измерения, '') as единицы_измерения
                FROM Позиции p
                LEFT JOIN Товары g ON p.ид_товара = g.ид_товара
                WHERE p.ид_документа = @id";

                    var param = command.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    command.Parameters.Add(param);

                    await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            позиции.Add(new PositionViewModel
                            {
                                id = reader.GetInt32(0),
                                goodsId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                quantity = reader.GetDouble(2),
                                price = reader.GetDecimal(3),
                                ставка_ндс = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4),
                                weight = reader.IsDBNull(5) ? 0 : (decimal)reader.GetDecimal(5),
                                packages = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                note = reader.IsDBNull(7) ? null : reader.GetString(7),
                                товар_наименование = reader.GetString(8),
                                единицы_измерения = reader.GetString(9)
                            });
                        }
                    }
                }

                var грузоотправитель = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
                var грузополучатель = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
                var перевозчик = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_перевозчика);
                var водитель = await _context.Водители
                    .FirstOrDefaultAsync(d => d.ид_водителя == документ.ид_водителя);
                var транспорт = await _context.Транспорт
                    .FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);
                var пунктПогрузки = await _context.Пункт_Погрузки
                    .FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
                var пунктРазгрузки = await _context.Пункт_Разгрузки
                    .FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);
                var типДокумента = await _context.Типы_Документов
                    .FirstOrDefaultAsync(t => t.ид_типа == документ.ид_типа);

                var итоги = new DocumentTotals();
                foreach (var pos in позиции)
                {
                    decimal cost = pos.price * (decimal)pos.quantity;
                    decimal vatRate = pos.ставка_ндс ?? 0;
                    decimal vatAmount = cost * (vatRate / 100);

                    итоги.ВсегоКоличество += (decimal)pos.quantity;
                    итоги.ВсегоСтоимость += cost;
                    итоги.ВсегоСуммаНДС += vatAmount;
                    итоги.ВсегоСтоимостьСНДС += cost + vatAmount;
                    итоги.ВсегоМасса += pos.weight;
                    итоги.ВсегоМест += pos.packages;
                }

                var model = new DocumentPreviewViewModel
                {
                    Документ = документ,
                    Позиции = позиции,
                    Грузоотправитель = грузоотправитель,
                    Грузополучатель = грузополучатель,
                    Перевозчик = перевозчик,
                    Водитель = водитель,
                    Транспорт = транспорт,
                    ПунктПогрузки = пунктПогрузки,
                    ПунктРазгрузки = пунктРазгрузки,
                    ТипДокумента = типДокумента,
                    Итоги = итоги,
                    ОснованиеОтпуска = "",
                    отпуск_разрешил = документ.отпуск_разрешил,
                    сдал_грузоотправитель = документ.сдал_грузоотправитель
                };

                string templateName = GetTemplateName(типДокумента?.краткое_наименование);

                return View($"~/Views/Shared/DocumentTemplates/{templateName}.cshtml", model);
            }
            catch (Exception ex)
            {
                return Content($"Ошибка: {ex.Message}<br><br>Stack trace:<br>{ex.StackTrace}", "text/html");
            }
        }

        private string GetTemplateName(string documentType)
        {
            return documentType?.ToUpper() switch
            {
                "ТТН" => "TTN1",
                "CMR" => "CMR",
                "ТН" => "TN2",
                _ => "TTN1"
            };
        }
    }
}