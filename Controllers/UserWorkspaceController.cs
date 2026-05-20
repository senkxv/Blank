using Blank.Data;
using Blank.Helpers;
using Blank.Models.Tables;
using Blank.Models.ViewModels;
using Blank.Models.Views;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using SelectPdf;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Spire.Pdf;
using Spire.Xls;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Body = DocumentFormat.OpenXml.Wordprocessing.Body;
using WordprocessingDocument = DocumentFormat.OpenXml.Packaging.WordprocessingDocument;

namespace Blank.Controllers
{
    public class UserWorkspaceController : Controller
    {
        private readonly ApplicationDBContext _context;

        public UserWorkspaceController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Route("/UserWorkspace/Error{statusCode}")]
        public IActionResult Error(int statusCode)
        {
            return statusCode switch
            {
                404 => View("Error404"),
                500 => View("Error500"),
                403 => View("Error403"),
                401 => View("Error401"),
                400 => View("Error400"),
                _ => View("Error500")
            };
        }

        public IActionResult Index()
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Authorization", "Login");
            }

            List<MainPage> данные;

            if (userOrgId.HasValue)
            {
                // Получаем список ID всех организаций, принадлежащих администратору
                var userOrgIds = _context.Организации
                    .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                    .Select(o => o.ид_организации)
                    .ToList();

                данные = _context.Документы
                    .Where(d => userOrgIds.Contains(d.ид_грузоотправителя)
                             || userOrgIds.Contains(d.ид_перевозчика)
                             || userOrgIds.Contains(d.ид_получателя))
                    .Select(d => new MainPage
                    {
                        ид_документа = d.ид_документа,
                        тип = _context.Типы_Документов
                            .Where(t => t.ид_типа == d.ид_типа)
                            .Select(t => t.краткое_наименование)
                            .FirstOrDefault(),
                        номер_документа = d.номер_документа,
                        дата_создания = d.дата_создания,
                        грузоотправитель = _context.Организации
                            .Where(o => o.ид_организации == d.ид_грузоотправителя)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        перевозчик = _context.Организации
                            .Where(o => o.ид_организации == d.ид_перевозчика)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        грузополучатель = _context.Организации
                            .Where(o => o.ид_организации == d.ид_получателя)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        пункт_погрузки = _context.Пункт_Погрузки
                            .Where(p => p.ид_пункта_погрузки == d.ид_пункта_погрузки)
                            .Select(p => p.наименование)
                            .FirstOrDefault(),
                        пункт_разгрузки = _context.Пункт_Разгрузки
                            .Where(p => p.ид_пункта_разгрузки == d.ид_пункта_разгрузки)
                            .Select(p => p.наименование)
                            .FirstOrDefault(),
                        ФИО_Водителя = _context.Водители
                            .Where(v => v.ид_водителя == d.ид_водителя)
                            .Select(v => v.фамилия + " " + v.имя + " " + v.отчество)
                            .FirstOrDefault(),
                        Марка_Машины = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.Марка_Транспорта.наименование_марки)
                            .FirstOrDefault(),
                        Регистрационный_Номер = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.регистрационный_номер)
                            .FirstOrDefault(),
                        Тип_ТС = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.Тип_Транспорта.наименование_типа)
                            .FirstOrDefault()
                    })
                    .ToList();
            }
            else
            {
                данные = new List<MainPage>();
            }

            return View(данные);
        }

        [HttpGet]
        public IActionResult CreateDocumentPage(int? routeId = null, int? currentPointIndex = null)
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? 106 : int.Parse(userOrgIdStr);

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            // Только организации пользователя
            ViewBag.Organizations = _context.Организации
                .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                .ToList();

            // Только маршруты организации пользователя
            ViewBag.AvailableRoutes = _context.Маршруты
                .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                .Where(r => r.ид_организации == userOrgId && r.статус == "активен")
                .ToList();
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
            ViewBag.UserOrgId = userOrgId;


            if (routeId.HasValue)
            {
                var route = _context.Маршруты
                    .Include(r => r.Водитель)
                    .Include(r => r.Транспорт)
                    .Include(r => r.Перевозчик)
                    .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                        .ThenInclude(t => t.ПунктПогрузки)
                    .Include(r => r.ТочкиМаршрута)
                        .ThenInclude(t => t.ПунктРазгрузки)
                    .FirstOrDefault(r => r.ид_маршрута == routeId && r.ид_организации == userOrgId);

                if (route != null)
                {
                    int completedDocs = currentPointIndex ?? _context.Документы.Count(d => d.ид_маршрута == routeId);

                    var currentPoint = route.ТочкиМаршрута
                        .OrderBy(t => t.порядковый_номер)
                        .Skip(completedDocs)
                        .FirstOrDefault();

                    ViewBag.DefaultDocumentType = route.ид_типа ?? 1;
                    ViewBag.SelectedRoute = route;
                    ViewBag.CurrentPoint = currentPoint;
                    ViewBag.CurrentPointIndex = completedDocs;
                    ViewBag.TotalPoints = route.ТочкиМаршрута.Count;
                    ViewBag.IsLastPoint = completedDocs >= route.ТочкиМаршрута.Count - 1;
                    ViewBag.RouteId = routeId;
                    ViewBag.DefaultSenderId = currentPoint?.ид_грузоотправителя;
                    ViewBag.DefaultReceiverId = currentPoint?.ид_грузополучателя;

                    var lastDoc = _context.Документы
                        .OrderByDescending(d => d.ид_документа)
                        .Select(d => d.номер_документа)
                        .FirstOrDefault();

                    string nextNumber = "000001";
                    if (!string.IsNullOrEmpty(lastDoc) && int.TryParse(lastDoc, out int lastNum))
                        nextNumber = (lastNum + 1).ToString("D6");

                    ViewBag.NextDocumentNumber = nextNumber;
                }
            }

            ViewBag.GoodsJson = JsonSerializer.Serialize(
                _context.Товары
                    .Where(g => g.ид_организации == userOrgId)
                    .Select(g => new {
                        ид_товара = g.ид_товара,
                        наименование = g.наименование,
                        единицы_измерения = g.единицы_измерения
                    })
                    .ToList()
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDocumentPage(Documents document, string positionsData, int? routeId = null, int? currentPointIndex = null, string action = "save")
        {
            try
            {
                if (action == "skip" && routeId.HasValue)
                {
                    var nextIndex = (currentPointIndex ?? 0) + 1;
                    TempData["Success"] = "Точка пропущена.";
                    return RedirectToAction("CreateDocumentPage", new { routeId = routeId, currentPointIndex = nextIndex });
                }

                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);
                var userIdStr = HttpContext.Session.GetString("UserId");
                var userId = int.Parse(userIdStr ?? "1");
                document.ид_пользователя = userId;

                if (routeId.HasValue)
                    document.ид_маршрута = routeId;

                var existingDoc = await _context.Документы
                    .FirstOrDefaultAsync(d => d.номер_документа == document.номер_документа
                                           && d.ид_грузоотправителя == userOrgId);
                if (existingDoc != null)
                {
                    ModelState.AddModelError("номер_документа", "Документ с таким номером уже существует в вашей организации");

                    ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
                    ViewBag.Organizations = _context.Организации.ToList();
                    ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
                    ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
                    ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
                    ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
                    ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
                    ViewBag.UserOrgId = userOrgId;

                    ViewBag.AvailableRoutes = _context.Маршруты
                        .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                        .Where(r => r.ид_организации == userOrgId && r.статус == "активен")
                        .ToList();

                    if (routeId.HasValue)
                    {
                        var route = _context.Маршруты
                            .Include(r => r.Водитель)
                            .Include(r => r.Транспорт)
                            .Include(r => r.Перевозчик)
                            .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                            .FirstOrDefault(r => r.ид_маршрута == routeId && r.ид_организации == userOrgId);

                        if (route != null)
                        {
                            ViewBag.DefaultDocumentType = route.ид_типа ?? 1;
                            ViewBag.SelectedRoute = route;
                            ViewBag.CurrentPointIndex = currentPointIndex ?? 0;
                            ViewBag.TotalPoints = route.ТочкиМаршрута.Count;
                            ViewBag.IsLastPoint = (currentPointIndex ?? 0) >= route.ТочкиМаршрута.Count - 1;
                            ViewBag.RouteId = routeId;
                        }
                    }

                    ViewBag.GoodsJson = JsonSerializer.Serialize(
                        _context.Товары.Where(g => g.ид_организации == userOrgId)
                            .Select(g => new { g.ид_товара, g.наименование, g.единицы_измерения })
                            .ToList()
                    );

                    return View(document);
                }

                if (document.дата_создания == default)
                    document.дата_создания = DateTime.Now;

                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                _context.Документы.Add(document);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

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
                                    continue;

                                decimal quantityDecimal = (decimal)pos.quantity;
                                decimal cost = pos.price * quantityDecimal;
                                decimal discountAmount = cost * (pos.discount / 100);
                                decimal costAfterDiscount = cost - discountAmount;
                                decimal vatAmount = costAfterDiscount * (pos.vatRate / 100);
                                decimal totalWithVat = costAfterDiscount + vatAmount;

                                var position = new Positions
                                {
                                    ид_документа = documentId,
                                    ид_товара = pos.goodsId,
                                    количество = pos.quantity,
                                    цена_за_единицу = pos.price,
                                    ставка_ндс = pos.vatRate,
                                    скидка = pos.discount,
                                    масса_груза = pos.weight,
                                    грузовых_мест = pos.packages > 0 ? pos.packages : (int?)null,
                                    примечание = string.IsNullOrEmpty(pos.note) ? null : pos.note,
                                    сумма_ндс = vatAmount,
                                    стоимость_с_ндс = totalWithVat
                                };

                                _context.Позиции.Add(position);
                                positionsSavedCount++;
                            }

                            if (positionsSavedCount > 0)
                                await _context.SaveChangesAsync();
                        }
                    }
                    catch (JsonException)
                    {
                        ModelState.AddModelError("", "Ошибка при обработке данных товаров");
                    }
                }

                if (action == "next" && routeId.HasValue)
                {
                    TempData["Success"] = $"Накладная №{document.номер_документа} сохранена! Добавлено позиций: {positionsSavedCount}";
                    return RedirectToAction("CreateDocumentPage", new { routeId = routeId });
                }

                TempData["Success"] = $"Документ №{document.номер_документа} успешно создан. Добавлено позиций: {positionsSavedCount}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var fullError = ex.Message;
                if (ex.InnerException != null)
                {
                    fullError += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                        fullError += " | Nested: " + ex.InnerException.InnerException.Message;
                }

                ModelState.AddModelError("", fullError);

                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

                ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
                ViewBag.Organizations = _context.Организации.ToList();
                ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
                ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
                ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
                ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
                ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
                ViewBag.UserOrgId = userOrgId;

                ViewBag.AvailableRoutes = _context.Маршруты
                    .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                    .Where(r => r.ид_организации == userOrgId && r.статус == "активен")
                    .ToList();

                if (routeId.HasValue)
                {
                    var route = _context.Маршруты
                        .Include(r => r.Водитель)
                        .Include(r => r.Транспорт)
                        .Include(r => r.Перевозчик)
                        .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                        .FirstOrDefault(r => r.ид_маршрута == routeId && r.ид_организации == userOrgId);

                    if (route != null)
                    {
                        ViewBag.SelectedRoute = route;
                        ViewBag.CurrentPointIndex = currentPointIndex ?? 0;
                        ViewBag.TotalPoints = route.ТочкиМаршрута.Count;
                        ViewBag.IsLastPoint = (currentPointIndex ?? 0) >= route.ТочкиМаршрута.Count - 1;
                        ViewBag.RouteId = routeId;
                    }
                }

                ViewBag.GoodsJson = JsonSerializer.Serialize(
                    _context.Товары.Where(g => g.ид_организации == userOrgId)
                        .Select(g => new { g.ид_товара, g.наименование, g.единицы_измерения })
                        .ToList()
                );

                ViewBag.NextDocumentNumber = document.номер_документа;

                return View(document);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditDocumentPage(int id)
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            var document = _context.Документы.Find(id);
            if (document == null)
            {
                return NotFound();
            }

            if (userOrgId.HasValue)
            {
                var userOrgIds = _context.Организации
                    .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                    .Select(o => o.ид_организации)
                    .ToList();

                if (!userOrgIds.Contains(document.ид_грузоотправителя)
                    && !userOrgIds.Contains(document.ид_перевозчика)
                    && !userOrgIds.Contains(document.ид_получателя))
                {
                    return NotFound();
                }
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();

            var goodsForJs = _context.Товары
                .Where(g => g.ид_организации == userOrgId)
                .Select(g => new {
                    ид_товара = g.ид_товара,
                    наименование = g.наименование,
                    единицы_измерения = g.единицы_измерения
                })
                .ToList();
            ViewBag.GoodsJson = JsonSerializer.Serialize(goodsForJs);

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

            if (document.ид_маршрута != null)
            {
                var route = await _context.Маршруты
                    .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                        .ThenInclude(t => t.ПунктПогрузки)
                    .Include(r => r.ТочкиМаршрута)
                        .ThenInclude(t => t.ПунктРазгрузки)
                    .FirstOrDefaultAsync(r => r.ид_маршрута == document.ид_маршрута);

                if (route != null)
                {
                    ViewBag.DocumentRoute = route;
                    ViewBag.TotalRoutePoints = route.ТочкиМаршрута.Count;

                    var pointIndex = await _context.Документы
                        .CountAsync(d => d.ид_маршрута == document.ид_маршрута && d.ид_документа < document.ид_документа);

                    ViewBag.RoutePointInfo = route.ТочкиМаршрута.Skip(pointIndex).FirstOrDefault();
                }
            }

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
                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

                var originalDoc = await _context.Документы.AsNoTracking().FirstOrDefaultAsync(d => d.ид_документа == id);

                var userOrgIds = _context.Организации
                    .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                    .Select(o => o.ид_организации)
                    .ToList();

                if (userOrgId.HasValue && originalDoc != null)
                {
                    if (!userOrgIds.Contains(originalDoc.ид_грузоотправителя)
                        && !userOrgIds.Contains(originalDoc.ид_перевозчика)
                        && !userOrgIds.Contains(originalDoc.ид_получателя))
                    {
                        return NotFound();
                    }
                }

                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                _context.Update(document);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

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
                                continue;

                            decimal quantityDecimal = (decimal)pos.quantity;
                            decimal cost = pos.price * quantityDecimal;
                            decimal discountAmount = cost * (pos.discount / 100);
                            decimal costAfterDiscount = cost - discountAmount;
                            decimal vatAmount = costAfterDiscount * (pos.vatRate / 100);
                            decimal totalWithVat = costAfterDiscount + vatAmount;

                            if (pos.id > 0)
                            {
                                var existing = await _context.Позиции.FindAsync(pos.id);
                                if (existing != null)
                                {
                                    existing.ид_товара = pos.goodsId;
                                    existing.количество = pos.quantity;
                                    existing.цена_за_единицу = pos.price;
                                    existing.ставка_ндс = pos.vatRate;
                                    existing.скидка = pos.discount;
                                    existing.масса_груза = pos.weight;
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
                                    ставка_ндс = pos.vatRate,
                                    скидка = pos.discount,
                                    масса_груза = pos.weight,
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

            var userOrgIdStr2 = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId2 = string.IsNullOrEmpty(userOrgIdStr2) ? null : int.Parse(userOrgIdStr2);

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId2).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId2).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId2).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId2).ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId2).ToList();

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
        public IActionResult ExportFullBackup()
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            if (userOrgId == null)
            {
                return Content("Ошибка: организация не определена. Войдите заново.");
            }

            var userOrgIds = _context.Организации
                .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                .Select(o => o.ид_организации)
                .ToList();

            using (var package = new ExcelPackage())
            {
                // Лист 1: Документы
                var sheetDocuments = package.Workbook.Worksheets.Add("Документы");
                sheetDocuments.Cells[1, 1].Value = "ид_документа";
                sheetDocuments.Cells[1, 2].Value = "номер_документа";
                sheetDocuments.Cells[1, 3].Value = "дата_создания";
                sheetDocuments.Cells[1, 4].Value = "ид_типа";
                sheetDocuments.Cells[1, 5].Value = "ид_грузоотправителя";
                sheetDocuments.Cells[1, 6].Value = "ид_перевозчика";
                sheetDocuments.Cells[1, 7].Value = "ид_получателя";
                sheetDocuments.Cells[1, 8].Value = "ид_пункта_погрузки";
                sheetDocuments.Cells[1, 9].Value = "ид_пункта_разгрузки";
                sheetDocuments.Cells[1, 10].Value = "ид_водителя";
                sheetDocuments.Cells[1, 11].Value = "ид_транспорта";
                sheetDocuments.Cells[1, 12].Value = "отпуск_разрешил";
                sheetDocuments.Cells[1, 13].Value = "сдал_грузоотправитель";

                var документы = _context.Документы
                    .Where(d => userOrgIds.Contains(d.ид_грузоотправителя)
                             || userOrgIds.Contains(d.ид_перевозчика)
                             || userOrgIds.Contains(d.ид_получателя))
                    .ToList();
                int row = 2;
                foreach (var doc in документы)
                {
                    sheetDocuments.Cells[row, 1].Value = doc.ид_документа;
                    sheetDocuments.Cells[row, 2].Value = doc.номер_документа;
                    sheetDocuments.Cells[row, 3].Value = doc.дата_создания.ToString("yyyy-MM-dd HH:mm:ss");
                    sheetDocuments.Cells[row, 4].Value = doc.ид_типа;
                    sheetDocuments.Cells[row, 5].Value = doc.ид_грузоотправителя;
                    sheetDocuments.Cells[row, 6].Value = doc.ид_перевозчика;
                    sheetDocuments.Cells[row, 7].Value = doc.ид_получателя;
                    sheetDocuments.Cells[row, 8].Value = doc.ид_пункта_погрузки;
                    sheetDocuments.Cells[row, 9].Value = doc.ид_пункта_разгрузки;
                    sheetDocuments.Cells[row, 10].Value = doc.ид_водителя;
                    sheetDocuments.Cells[row, 11].Value = doc.ид_транспорта;
                    sheetDocuments.Cells[row, 12].Value = doc.отпуск_разрешил ?? "";
                    sheetDocuments.Cells[row, 13].Value = doc.сдал_грузоотправитель ?? "";
                    row++;
                }
                sheetDocuments.Cells.AutoFitColumns();

                // Лист 2: Позиции
                var sheetPositions = package.Workbook.Worksheets.Add("Позиции");
                sheetPositions.Cells[1, 1].Value = "ид_позиции";
                sheetPositions.Cells[1, 2].Value = "ид_документа";
                sheetPositions.Cells[1, 3].Value = "ид_товара";
                sheetPositions.Cells[1, 4].Value = "количество";
                sheetPositions.Cells[1, 5].Value = "цена_за_единицу";
                sheetPositions.Cells[1, 6].Value = "ставка_ндс";
                sheetPositions.Cells[1, 7].Value = "скидка";
                sheetPositions.Cells[1, 8].Value = "масса_груза";
                sheetPositions.Cells[1, 9].Value = "грузовых_мест";
                sheetPositions.Cells[1, 10].Value = "примечание";
                sheetPositions.Cells[1, 11].Value = "сумма_ндс";
                sheetPositions.Cells[1, 12].Value = "стоимость_с_ндс";

                var докIds = документы.Select(d => d.ид_документа).ToList();
                var позиции = _context.Позиции.Where(p => докIds.Contains(p.ид_документа)).ToList();
                row = 2;
                foreach (var pos in позиции)
                {
                    sheetPositions.Cells[row, 1].Value = pos.ид_позиции;
                    sheetPositions.Cells[row, 2].Value = pos.ид_документа;
                    sheetPositions.Cells[row, 3].Value = pos.ид_товара;
                    sheetPositions.Cells[row, 4].Value = pos.количество;
                    sheetPositions.Cells[row, 5].Value = pos.цена_за_единицу;
                    sheetPositions.Cells[row, 6].Value = pos.ставка_ндс;
                    sheetPositions.Cells[row, 7].Value = pos.скидка;
                    sheetPositions.Cells[row, 8].Value = pos.масса_груза;
                    sheetPositions.Cells[row, 9].Value = pos.грузовых_мест;
                    sheetPositions.Cells[row, 10].Value = pos.примечание ?? "";
                    sheetPositions.Cells[row, 11].Value = pos.сумма_ндс;
                    sheetPositions.Cells[row, 12].Value = pos.стоимость_с_ндс;
                    row++;
                }
                sheetPositions.Cells.AutoFitColumns();

                // Лист 3: Товары
                var sheetGoods = package.Workbook.Worksheets.Add("Товары");
                sheetGoods.Cells[1, 1].Value = "ид_товара";
                sheetGoods.Cells[1, 2].Value = "код_товара";
                sheetGoods.Cells[1, 3].Value = "наименование";
                sheetGoods.Cells[1, 4].Value = "единицы_измерения";
                sheetGoods.Cells[1, 5].Value = "ид_организации";

                var товары = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
                row = 2;
                foreach (var товар in товары)
                {
                    sheetGoods.Cells[row, 1].Value = товар.ид_товара;
                    sheetGoods.Cells[row, 2].Value = товар.код_товара ?? "";
                    sheetGoods.Cells[row, 3].Value = товар.наименование;
                    sheetGoods.Cells[row, 4].Value = товар.единицы_измерения;
                    sheetGoods.Cells[row, 5].Value = товар.ид_организации;
                    row++;
                }
                sheetGoods.Cells.AutoFitColumns();

                // Лист 4: Организации
                var sheetOrganizations = package.Workbook.Worksheets.Add("Организации");
                sheetOrganizations.Cells[1, 1].Value = "ид_организации";
                sheetOrganizations.Cells[1, 2].Value = "название";
                sheetOrganizations.Cells[1, 3].Value = "унп";
                sheetOrganizations.Cells[1, 4].Value = "адрес";
                sheetOrganizations.Cells[1, 5].Value = "почта";
                sheetOrganizations.Cells[1, 6].Value = "ид_владельца";

                var организации = _context.Организации.Where(o => userOrgIds.Contains(o.ид_организации)).ToList();
                row = 2;
                foreach (var org in организации)
                {
                    sheetOrganizations.Cells[row, 1].Value = org.ид_организации;
                    sheetOrganizations.Cells[row, 2].Value = org.название;
                    sheetOrganizations.Cells[row, 3].Value = org.унп;
                    sheetOrganizations.Cells[row, 4].Value = org.адрес;
                    sheetOrganizations.Cells[row, 5].Value = org.почта;
                    sheetOrganizations.Cells[row, 6].Value = org.ид_владельца;
                    row++;
                }
                sheetOrganizations.Cells.AutoFitColumns();

                // Лист 5: Водители
                var sheetDrivers = package.Workbook.Worksheets.Add("Водители");
                sheetDrivers.Cells[1, 1].Value = "ид_водителя";
                sheetDrivers.Cells[1, 2].Value = "фамилия";
                sheetDrivers.Cells[1, 3].Value = "имя";
                sheetDrivers.Cells[1, 4].Value = "отчество";
                sheetDrivers.Cells[1, 5].Value = "номер_лицензии";
                sheetDrivers.Cells[1, 6].Value = "ид_организации";

                var водители = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
                row = 2;
                foreach (var driver in водители)
                {
                    sheetDrivers.Cells[row, 1].Value = driver.ид_водителя;
                    sheetDrivers.Cells[row, 2].Value = driver.фамилия;
                    sheetDrivers.Cells[row, 3].Value = driver.имя;
                    sheetDrivers.Cells[row, 4].Value = driver.отчество;
                    sheetDrivers.Cells[row, 5].Value = driver.номер_лицензии;
                    sheetDrivers.Cells[row, 6].Value = driver.ид_организации;
                    row++;
                }
                sheetDrivers.Cells.AutoFitColumns();

                // Лист 6: Транспорт
                var sheetTransport = package.Workbook.Worksheets.Add("Транспорт");
                sheetTransport.Cells[1, 1].Value = "ид_транспорта";
                sheetTransport.Cells[1, 2].Value = "регистрационный_номер";
                sheetTransport.Cells[1, 3].Value = "ид_марки";
                sheetTransport.Cells[1, 4].Value = "ид_типа_транспорта";
                sheetTransport.Cells[1, 5].Value = "ид_организации";

                var транспорт = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
                row = 2;
                foreach (var t in транспорт)
                {
                    sheetTransport.Cells[row, 1].Value = t.ид_транспорта;
                    sheetTransport.Cells[row, 2].Value = t.регистрационный_номер;
                    sheetTransport.Cells[row, 3].Value = t.ид_марки;
                    sheetTransport.Cells[row, 4].Value = t.ид_типа_транспорта;
                    sheetTransport.Cells[row, 5].Value = t.ид_организации;
                    row++;
                }
                sheetTransport.Cells.AutoFitColumns();

                // Лист 7: Пункты погрузки
                var sheetLoadingPoints = package.Workbook.Worksheets.Add("ПунктыПогрузки");
                sheetLoadingPoints.Cells[1, 1].Value = "ид_пункта_погрузки";
                sheetLoadingPoints.Cells[1, 2].Value = "наименование";
                sheetLoadingPoints.Cells[1, 3].Value = "адрес";
                sheetLoadingPoints.Cells[1, 4].Value = "ид_организации";

                var loadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
                row = 2;
                foreach (var point in loadingPoints)
                {
                    sheetLoadingPoints.Cells[row, 1].Value = point.ид_пункта_погрузки;
                    sheetLoadingPoints.Cells[row, 2].Value = point.наименование;
                    sheetLoadingPoints.Cells[row, 3].Value = point.адрес;
                    sheetLoadingPoints.Cells[row, 4].Value = point.ид_организации;
                    row++;
                }
                sheetLoadingPoints.Cells.AutoFitColumns();

                // Лист 8: Пункты разгрузки
                var sheetUnloadingPoints = package.Workbook.Worksheets.Add("ПунктыРазгрузки");
                sheetUnloadingPoints.Cells[1, 1].Value = "ид_пункта_разгрузки";
                sheetUnloadingPoints.Cells[1, 2].Value = "наименование";
                sheetUnloadingPoints.Cells[1, 3].Value = "адрес";
                sheetUnloadingPoints.Cells[1, 4].Value = "ид_организации";

                var unloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
                row = 2;
                foreach (var point in unloadingPoints)
                {
                    sheetUnloadingPoints.Cells[row, 1].Value = point.ид_пункта_разгрузки;
                    sheetUnloadingPoints.Cells[row, 2].Value = point.наименование;
                    sheetUnloadingPoints.Cells[row, 3].Value = point.адрес;
                    sheetUnloadingPoints.Cells[row, 4].Value = point.ид_организации;
                    row++;
                }
                sheetUnloadingPoints.Cells.AutoFitColumns();

                // Лист 9: Типы документов (общие)
                var sheetDocTypes = package.Workbook.Worksheets.Add("ТипыДокументов");
                sheetDocTypes.Cells[1, 1].Value = "ид_типа";
                sheetDocTypes.Cells[1, 2].Value = "краткое_наименование";
                sheetDocTypes.Cells[1, 3].Value = "полное_наименование";

                var docTypes = _context.Типы_Документов.ToList();
                row = 2;
                foreach (var type in docTypes)
                {
                    sheetDocTypes.Cells[row, 1].Value = type.ид_типа;
                    sheetDocTypes.Cells[row, 2].Value = type.краткое_наименование;
                    sheetDocTypes.Cells[row, 3].Value = type.полное_наименование;
                    row++;
                }
                sheetDocTypes.Cells.AutoFitColumns();

                // Лист: Марки транспорта — только используемые организацией
                var sheetMarks = package.Workbook.Worksheets.Add("МаркиТранспорта");
                sheetMarks.Cells[1, 1].Value = "ид_марки";
                sheetMarks.Cells[1, 2].Value = "наименование_марки";

                var usedBrandIds = _context.Транспорт
                    .Where(t => t.ид_организации == userOrgId)
                    .Select(t => t.ид_марки)
                    .Distinct()
                    .ToList();

                var marks = _context.Марка_Транспорта
                    .Where(m => usedBrandIds.Contains(m.ид_марки))
                    .ToList();
                row = 2;
                foreach (var mark in marks)
                {
                    sheetMarks.Cells[row, 1].Value = mark.ид_марки;
                    sheetMarks.Cells[row, 2].Value = mark.наименование_марки;
                    row++;
                }
                sheetMarks.Cells.AutoFitColumns();

                // Лист: Типы транспорта — только используемые организацией
                var sheetTransportTypesExport = package.Workbook.Worksheets.Add("ТипыТранспорта");
                sheetTransportTypesExport.Cells[1, 1].Value = "ид_типа_транспорта";
                sheetTransportTypesExport.Cells[1, 2].Value = "наименование_типа";

                var usedTypeIds = _context.Транспорт
                    .Where(t => t.ид_организации == userOrgId)
                    .Select(t => t.ид_типа_транспорта)
                    .Distinct()
                    .ToList();

                var types = _context.Тип_Транспорта
                    .Where(t => usedTypeIds.Contains(t.ид_типа_транспорта))
                    .ToList();
                row = 2;
                foreach (var type in types)
                {
                    sheetTransportTypesExport.Cells[row, 1].Value = type.ид_типа_транспорта;
                    sheetTransportTypesExport.Cells[row, 2].Value = type.наименование_типа;
                    row++;
                }
                sheetTransportTypesExport.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RestoreFromBackup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RestoreFromBackup(IFormFile file)
        {
            var currentUserId = HttpContext.Session.GetString("UserOrgId");
            int? ownerId = string.IsNullOrEmpty(currentUserId) ? null : int.Parse(currentUserId);

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Пожалуйста, выберите файл для восстановления";
                return RedirectToAction("Index");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
            {
                TempData["Error"] = "Поддерживаются только файлы .xlsx";
                return RedirectToAction("Index");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");

                                int countDocs = 0;
                                int countPositions = 0;
                                Dictionary<int, int> docIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> goodsIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> driverIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> transportIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> loadingPointIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> unloadingPointIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> orgIdMapping = new Dictionary<int, int>();

                                // 1. Организации
                                var sheetOrgs = package.Workbook.Worksheets["Организации"];
                                if (sheetOrgs?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetOrgs.Dimension.Rows; row++)
                                    {
                                        var id = sheetOrgs.Cells[row, 1]?.Value;
                                        var name = sheetOrgs.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;
                                        int oldId = Convert.ToInt32(id);

                                        // Проверяем, существует ли уже такая организация
                                        var existing = await _context.Организации.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Организации (ид_организации, название, унп, адрес, почта, ид_владельца) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                                                oldId, name,
                                                sheetOrgs.Cells[row, 3]?.Value?.ToString() ?? "",
                                                sheetOrgs.Cells[row, 4]?.Value?.ToString() ?? "",
                                                sheetOrgs.Cells[row, 5]?.Value?.ToString() ?? "",
                                                ownerId);
                                        }
                                        orgIdMapping[oldId] = oldId; // ID остаётся прежним
                                    }
                                }

                                // 2. Товары
                                var sheetGoods = package.Workbook.Worksheets["Товары"];
                                if (sheetGoods?.Dimension?.Rows > 1)
                                {
                                    var header2 = sheetGoods.Cells[1, 2]?.Value?.ToString() ?? "";
                                    bool hasCodeColumn = header2 == "код_товара";
                                    for (int row = 2; row <= sheetGoods.Dimension.Rows; row++)
                                    {
                                        var id = sheetGoods.Cells[row, 1]?.Value;
                                        if (id == null) continue;
                                        int oldId = Convert.ToInt32(id);
                                        string code = "", name = "", unit = "";
                                        if (hasCodeColumn)
                                        {
                                            code = sheetGoods.Cells[row, 2]?.Value?.ToString() ?? "";
                                            name = sheetGoods.Cells[row, 3]?.Value?.ToString() ?? "";
                                            unit = sheetGoods.Cells[row, 4]?.Value?.ToString() ?? "";
                                        }
                                        else
                                        {
                                            name = sheetGoods.Cells[row, 2]?.Value?.ToString() ?? "";
                                            unit = sheetGoods.Cells[row, 3]?.Value?.ToString() ?? "";
                                        }
                                        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code)) continue;

                                        var existing = await _context.Товары.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Товары (ид_товара, код_товара, наименование, единицы_измерения, ид_организации) VALUES ({0}, {1}, {2}, {3}, {4})",
                                                oldId, code, name, unit, ownerId);
                                        }
                                        goodsIdMapping[oldId] = oldId;
                                    }
                                }

                                // 3. Водители
                                var sheetDrivers = package.Workbook.Worksheets["Водители"];
                                if (sheetDrivers?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetDrivers.Dimension.Rows; row++)
                                    {
                                        var id = sheetDrivers.Cells[row, 1]?.Value;
                                        var lastName = sheetDrivers.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(lastName)) continue;
                                        int oldId = Convert.ToInt32(id);

                                        var existing = await _context.Водители.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Водители (ид_водителя, фамилия, имя, отчество, номер_лицензии, ид_организации) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                                                oldId, lastName,
                                                sheetDrivers.Cells[row, 3]?.Value?.ToString() ?? "",
                                                sheetDrivers.Cells[row, 4]?.Value?.ToString() ?? "",
                                                sheetDrivers.Cells[row, 5]?.Value?.ToString() ?? "",
                                                ownerId);
                                        }
                                        driverIdMapping[oldId] = oldId;
                                    }
                                }

                                // 4. Транспорт
                                var sheetTransport = package.Workbook.Worksheets["Транспорт"];
                                if (sheetTransport?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetTransport.Dimension.Rows; row++)
                                    {
                                        var id = sheetTransport.Cells[row, 1]?.Value;
                                        var regNumber = sheetTransport.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(regNumber)) continue;
                                        int oldId = Convert.ToInt32(id);

                                        var existing = await _context.Транспорт.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Транспорт (ид_транспорта, регистрационный_номер, ид_марки, ид_типа_транспорта, ид_организации) VALUES ({0}, {1}, {2}, {3}, {4})",
                                                oldId, regNumber,
                                                sheetTransport.Cells[row, 3]?.Value ?? 1,
                                                sheetTransport.Cells[row, 4]?.Value ?? 1,
                                                ownerId);
                                        }
                                        transportIdMapping[oldId] = oldId;
                                    }
                                }

                                // 5. Пункты погрузки
                                var sheetLoading = package.Workbook.Worksheets["ПунктыПогрузки"];
                                if (sheetLoading?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetLoading.Dimension.Rows; row++)
                                    {
                                        var id = sheetLoading.Cells[row, 1]?.Value;
                                        var name = sheetLoading.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;
                                        int oldId = Convert.ToInt32(id);

                                        var existing = await _context.Пункт_Погрузки.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Пункт_Погрузки (ид_пункта_погрузки, наименование, адрес, ид_организации) VALUES ({0}, {1}, {2}, {3})",
                                                oldId, name,
                                                sheetLoading.Cells[row, 3]?.Value?.ToString() ?? "",
                                                ownerId);
                                        }
                                        loadingPointIdMapping[oldId] = oldId;
                                    }
                                }

                                // 6. Пункты разгрузки
                                var sheetUnloading = package.Workbook.Worksheets["ПунктыРазгрузки"];
                                if (sheetUnloading?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetUnloading.Dimension.Rows; row++)
                                    {
                                        var id = sheetUnloading.Cells[row, 1]?.Value;
                                        var name = sheetUnloading.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;
                                        int oldId = Convert.ToInt32(id);

                                        var existing = await _context.Пункт_Разгрузки.FindAsync(oldId);
                                        if (existing == null)
                                        {
                                            await _context.Database.ExecuteSqlRawAsync(
                                                "INSERT INTO Пункт_Разгрузки (ид_пункта_разгрузки, наименование, адрес, ид_организации) VALUES ({0}, {1}, {2}, {3})",
                                                oldId, name,
                                                sheetUnloading.Cells[row, 3]?.Value?.ToString() ?? "",
                                                ownerId);
                                        }
                                        unloadingPointIdMapping[oldId] = oldId;
                                    }
                                }

                                // 7. Документы (через EF Core для точного получения ID)
                                var sheetDocs = package.Workbook.Worksheets["Документы"];
                                if (sheetDocs?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetDocs.Dimension.Rows; row++)
                                    {
                                        var docNumberCell = sheetDocs.Cells[row, 2]?.Value;
                                        if (docNumberCell == null) continue;
                                        string docNumber = docNumberCell.ToString().Trim();
                                        if (string.IsNullOrEmpty(docNumber)) continue;

                                        int originalDocId = 0;
                                        var docIdObj = sheetDocs.Cells[row, 1]?.Value;
                                        if (docIdObj != null) int.TryParse(docIdObj.ToString(), out originalDocId);

                                        DateTime docDate = DateTime.Now;
                                        var dateObj = sheetDocs.Cells[row, 3]?.Value;
                                        if (dateObj != null) DateTime.TryParse(dateObj.ToString(), out docDate);

                                        int typeId = 1;
                                        var typeObj = sheetDocs.Cells[row, 4]?.Value;
                                        if (typeObj != null) int.TryParse(typeObj.ToString(), out typeId);
                                        if (typeId < 1) typeId = 1;

                                        int senderId = GetIntValue(sheetDocs.Cells[row, 5]?.Value);
                                        int carrierId = GetIntValue(sheetDocs.Cells[row, 6]?.Value);
                                        int receiverId = GetIntValue(sheetDocs.Cells[row, 7]?.Value);
                                        int loadingPointId = GetIntValue(sheetDocs.Cells[row, 8]?.Value);
                                        int unloadingPointId = GetIntValue(sheetDocs.Cells[row, 9]?.Value);
                                        int driverId = GetIntValue(sheetDocs.Cells[row, 10]?.Value);
                                        int transportId = GetIntValue(sheetDocs.Cells[row, 11]?.Value);
                                        string отпускРазрешил = sheetDocs.Cells[row, 12]?.Value?.ToString() ?? "";
                                        string сдалГрузоотправитель = sheetDocs.Cells[row, 13]?.Value?.ToString() ?? "";

                                        // Избегаем дубликатов номера внутри той же организации
                                        bool номерЗанят = await _context.Документы
                                            .AnyAsync(d => d.номер_документа == docNumber && d.ид_грузоотправителя == senderId);
                                        if (номерЗанят)
                                            docNumber += "_import";

                                        var newDoc = new Documents
                                        {
                                            номер_документа = docNumber,
                                            дата_создания = docDate,
                                            ид_типа = typeId,
                                            ид_грузоотправителя = senderId,
                                            ид_перевозчика = carrierId,
                                            ид_получателя = receiverId,
                                            ид_пункта_погрузки = loadingPointId,
                                            ид_пункта_разгрузки = unloadingPointId,
                                            ид_водителя = driverId,
                                            ид_транспорта = transportId,
                                            отпуск_разрешил = отпускРазрешил,
                                            сдал_грузоотправитель = сдалГрузоотправитель,
                                            ид_пользователя = 1
                                        };

                                        _context.Документы.Add(newDoc);
                                        await _context.SaveChangesAsync();

                                        docIdMapping[originalDocId] = newDoc.ид_документа;
                                        countDocs++;
                                    }
                                }

                                // 8. Позиции
                                var sheetPositions = package.Workbook.Worksheets["Позиции"];
                                if (sheetPositions?.Dimension?.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetPositions.Dimension.Rows; row++)
                                    {
                                        var docIdObj = sheetPositions.Cells[row, 2]?.Value;
                                        if (docIdObj == null) continue;
                                        int oldDocId = Convert.ToInt32(docIdObj);
                                        if (!docIdMapping.ContainsKey(oldDocId)) continue;
                                        int docId = docIdMapping[oldDocId];

                                        int goodsId = GetIntValue(sheetPositions.Cells[row, 3]?.Value);
                                        double quantity = 0;
                                        if (sheetPositions.Cells[row, 4]?.Value != null)
                                            double.TryParse(sheetPositions.Cells[row, 4].Value.ToString(), out quantity);
                                        decimal price = 0;
                                        if (sheetPositions.Cells[row, 5]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 5].Value.ToString(), out price);

                                        decimal vat = 0;
                                        if (sheetPositions.Cells[row, 6]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 6].Value.ToString(), out vat);
                                        decimal discount = 0;
                                        if (sheetPositions.Cells[row, 7]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 7].Value.ToString(), out discount);
                                        decimal weight = 0;
                                        if (sheetPositions.Cells[row, 8]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 8].Value.ToString(), out weight);
                                        int packages = 0;
                                        if (sheetPositions.Cells[row, 9]?.Value != null)
                                            int.TryParse(sheetPositions.Cells[row, 9].Value.ToString(), out packages);
                                        string note = sheetPositions.Cells[row, 10]?.Value?.ToString() ?? "";
                                        decimal vatSum = 0;
                                        if (sheetPositions.Cells[row, 11]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 11].Value.ToString(), out vatSum);
                                        decimal total = 0;
                                        if (sheetPositions.Cells[row, 12]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 12].Value.ToString(), out total);

                                        var newPos = new Positions
                                        {
                                            ид_документа = docId,
                                            ид_товара = goodsId,
                                            количество = quantity,
                                            цена_за_единицу = price,
                                            ставка_ндс = vat,
                                            скидка = discount,
                                            масса_груза = weight,
                                            грузовых_мест = packages > 0 ? packages : (int?)null,
                                            примечание = string.IsNullOrEmpty(note) ? null : note,
                                            сумма_ндс = vatSum,
                                            стоимость_с_ндс = total
                                        };
                                        _context.Позиции.Add(newPos);
                                        countPositions++;
                                    }
                                    await _context.SaveChangesAsync();
                                }

                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
                                await transaction.CommitAsync();

                                TempData["Success"] = $"Восстановлено: Документов: {countDocs}, Позиций: {countPositions}";
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
                                TempData["Error"] = $"Ошибка: {ex.Message}<br/>{ex.InnerException?.Message}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Вспомогательный метод для безопасного получения int значения
        private int GetIntValue(object value)
        {
            if (value == null) return 1;
            if (int.TryParse(value.ToString(), out int result))
            {
                return result > 0 ? result : 1;
            }
            return 1;
        }

        [HttpGet]
        public async Task<IActionResult> DebugExcel()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "FullBackup_20260502_194201.xlsx");

                if (!System.IO.File.Exists(filePath))
                {
                    return Content($"Файл не найден: {filePath}");
                }

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var sheet = package.Workbook.Worksheets["Документы"];
                    if (sheet == null) return Content("Лист 'Документы' не найден");

                    var result = new StringBuilder();
                    result.AppendLine($"<h3>Отладка Excel файла</h3>");
                    result.AppendLine($"<b>Всего строк в Dimension: {sheet.Dimension?.Rows}</b><br/>");

                    // Проходим по всем строкам и выводим, что видим
                    for (int row = 1; row <= 20; row++)
                    {
                        var cellValue = sheet.Cells[row, 3]?.Value;
                        string hasValue = cellValue == null ? "NULL" : $"'{cellValue.ToString()}'";
                        result.AppendLine($"Строка {row}: значение в колонке 3 = {hasValue}<br/>");
                    }

                    return Content(result.ToString(), "text/html");
                }
            }
            catch (Exception ex)
            {
                return Content($"Ошибка: {ex.Message}<br/>{ex.StackTrace}");
            }
        }


        private async Task RestoreReferenceTables(ExcelPackage package)
        {
            // Отключаем проверку внешних ключей (MySQL)
            await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");

            try
            {
                // 1. Типы документов
                var sheetTypes = package.Workbook.Worksheets["ТипыДокументов"];
                if (sheetTypes?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetTypes.Dimension.Rows; r++)
                    {
                        _context.Типы_Документов.Add(new Document_Type
                        {
                            краткое_наименование = sheetTypes.Cells[r, 2].Text?.Trim(),
                            полное_наименование = sheetTypes.Cells[r, 3].Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 2. Организации
                var sheetOrg = package.Workbook.Worksheets["Организации"];
                if (sheetOrg?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetOrg.Dimension.Rows; r++)
                    {
                        _context.Организации.Add(new Organization
                        {
                            название = sheetOrg.Cells[r, 2].Text?.Trim(),
                            унп = sheetOrg.Cells[r, 3].Text?.Trim(),
                            адрес = sheetOrg.Cells[r, 4].Text?.Trim(),
                            почта = sheetOrg.Cells[r, 5]?.Text?.Trim() ?? ""
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 3. Товары
                var sheetGoods = package.Workbook.Worksheets["Товары"];
                if (sheetGoods?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetGoods.Dimension.Rows; r++)
                    {
                        _context.Товары.Add(new Goods
                        {
                            наименование = sheetGoods.Cells[r, 2].Text?.Trim(),
                            единицы_измерения = sheetGoods.Cells[r, 3].Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 4. Водители
                var sheetDrivers = package.Workbook.Worksheets["Водители"];
                if (sheetDrivers?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetDrivers.Dimension.Rows; r++)
                    {
                        _context.Водители.Add(new Drivers
                        {
                            фамилия = sheetDrivers.Cells[r, 2].Text?.Trim(),
                            имя = sheetDrivers.Cells[r, 3].Text?.Trim(),
                            отчество = sheetDrivers.Cells[r, 4].Text?.Trim(),
                            номер_лицензии = sheetDrivers.Cells[r, 5]?.Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. Транспорт
                var sheetTransport = package.Workbook.Worksheets["Транспорт"];
                if (sheetTransport?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetTransport.Dimension.Rows; r++)
                    {
                        _context.Транспорт.Add(new Transport
                        {
                            регистрационный_номер = sheetTransport.Cells[r, 2].Text?.Trim(),
                            ид_марки = int.TryParse(sheetTransport.Cells[r, 3].Text, out var mid) ? mid : 0,
                            ид_типа_транспорта = int.TryParse(sheetTransport.Cells[r, 5].Text, out var tid) ? tid : 0
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 6. Пункты погрузки
                var sheetLoading = package.Workbook.Worksheets["ПунктыПогрузки"];
                if (sheetLoading?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetLoading.Dimension.Rows; r++)
                    {
                        _context.Пункт_Погрузки.Add(new Loading_Point
                        {
                            наименование = sheetLoading.Cells[r, 2].Text?.Trim(),
                            адрес = sheetLoading.Cells[r, 3].Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 7. Пункты разгрузки
                var sheetUnloading = package.Workbook.Worksheets["ПунктыРазгрузки"];
                if (sheetUnloading?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetUnloading.Dimension.Rows; r++)
                    {
                        _context.Пункт_Разгрузки.Add(new Unloading_Point
                        {
                            наименование = sheetUnloading.Cells[r, 2].Text?.Trim(),
                            адрес = sheetUnloading.Cells[r, 3].Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 8. Марки транспорта (если нужен отдельный лист)
                var sheetMarks = package.Workbook.Worksheets["Марка_Транспорта"]; // если есть
                if (sheetMarks?.Dimension?.Rows > 1)
                {
                    for (int r = 2; r <= sheetMarks.Dimension.Rows; r++)
                    {
                        _context.Марка_Транспорта.Add(new Transport_Mark
                        {
                            наименование_марки = sheetMarks.Cells[r, 2].Text?.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }
            finally
            {
                // Включаем обратно проверку внешних ключей
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
            }
        }

        private object GetSafeCellValue(ExcelRange cell)
        {
            try
            {
                if (cell == null) return null;
                if (cell.Value == null) return null;
                return cell.Value;
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchString)
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            List<MainPage> данные;

            if (userOrgId.HasValue)
            {
                var userOrgIds = _context.Организации
                    .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                    .Select(o => o.ид_организации)
                    .ToList();

                данные = await _context.Документы
                    .Where(d => userOrgIds.Contains(d.ид_грузоотправителя)
                             || userOrgIds.Contains(d.ид_перевозчика)
                             || userOrgIds.Contains(d.ид_получателя))
                    .Select(d => new MainPage
                    {
                        ид_документа = d.ид_документа,
                        тип = _context.Типы_Документов
                            .Where(t => t.ид_типа == d.ид_типа)
                            .Select(t => t.краткое_наименование)
                            .FirstOrDefault(),
                        номер_документа = d.номер_документа,
                        дата_создания = d.дата_создания,
                        грузоотправитель = _context.Организации
                            .Where(o => o.ид_организации == d.ид_грузоотправителя)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        перевозчик = _context.Организации
                            .Where(o => o.ид_организации == d.ид_перевозчика)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        грузополучатель = _context.Организации
                            .Where(o => o.ид_организации == d.ид_получателя)
                            .Select(o => o.название)
                            .FirstOrDefault(),
                        пункт_погрузки = _context.Пункт_Погрузки
                            .Where(p => p.ид_пункта_погрузки == d.ид_пункта_погрузки)
                            .Select(p => p.наименование)
                            .FirstOrDefault(),
                        пункт_разгрузки = _context.Пункт_Разгрузки
                            .Where(p => p.ид_пункта_разгрузки == d.ид_пункта_разгрузки)
                            .Select(p => p.наименование)
                            .FirstOrDefault(),
                        ФИО_Водителя = _context.Водители
                            .Where(v => v.ид_водителя == d.ид_водителя)
                            .Select(v => v.фамилия + " " + v.имя + " " + v.отчество)
                            .FirstOrDefault(),
                        Марка_Машины = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.Марка_Транспорта.наименование_марки)
                            .FirstOrDefault(),
                        Регистрационный_Номер = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.регистрационный_номер)
                            .FirstOrDefault(),
                        Тип_ТС = _context.Транспорт
                            .Where(t => t.ид_транспорта == d.ид_транспорта)
                            .Select(t => t.Тип_Транспорта.наименование_типа)
                            .FirstOrDefault()
                    })
                    .ToListAsync();
            }
            else
            {
                данные = new List<MainPage>();
            }

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
        [Route("Print/{documentNumber}")]
        public async Task<IActionResult> PrintByNumber(string documentNumber)
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            // Получаем список ID организаций пользователя
            var userOrgIds = await _context.Организации
                .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                .Select(o => o.ид_организации)
                .ToListAsync();

            // Ищем документ, который принадлежит одной из организаций пользователя
            var doc = await _context.Документы.FirstOrDefaultAsync(d =>
                d.номер_документа == documentNumber &&
                (userOrgIds.Contains(d.ид_грузоотправителя) ||
                 userOrgIds.Contains(d.ид_перевозчика) ||
                 userOrgIds.Contains(d.ид_получателя)));

            if (doc == null) return NotFound();

            var docType = await _context.Типы_Документов
                .Where(t => t.ид_типа == doc.ид_типа)
                .Select(t => t.краткое_наименование)
                .FirstOrDefaultAsync();

            if (docType?.ToUpper() == "ТТН")
                return await GenerateTtnPdf(doc);
            else if (docType?.ToUpper() == "CMR")
                return await GenerateCmrPdf(doc);
            else if (docType?.ToUpper() == "ТН")
                return await GenerateTnPdf(doc);
            else
                return await GeneratePrintPdf(doc);
        }

        // В начало файла UserWorkspaceController.cs необходимо добавить:
        // using System.Drawing;

        // В начало файла UserWorkspaceController.cs необходимо добавить:
        // using System.Drawing;

        private async Task<IActionResult> GenerateTtnPdf(Documents документ)
        {
            // 1. Получение позиций и связанных данных
            var позиции = await _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == документ.ид_документа)
                .ToListAsync();

            var грузоотправитель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
            var грузополучатель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
            var водитель = await _context.Водители.FirstOrDefaultAsync(d => d.ид_водителя == документ.ид_водителя);
            var транспорт = await _context.Транспорт.FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);
            if (транспорт != null)
                await _context.Entry(транспорт).Reference(t => t.Марка_Транспорта).LoadAsync();
            var пунктПогрузки = await _context.Пункт_Погрузки.FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
            var пунктРазгрузки = await _context.Пункт_Разгрузки.FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);

            // 2. Подсчёт итогов
            // 2. Подсчёт итогов (с НДС и без)
            decimal totalWeight = 0;
            decimal totalCost = 0;          // стоимость без НДС
            decimal totalNds = 0;           // сумма НДС
            decimal totalQuantity = позиции.Sum(p => (decimal)p.количество);
            decimal totalSumWithNds = 0;   // стоимость с НДС

            foreach (var pos in позиции)
            {
                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);

                totalCost += afterDiscount;
                totalNds += vat;
                totalSumWithNds += afterDiscount + vat;
                totalWeight += pos.масса_груза ?? 0;
            }

            // Выбираем, что подставлять в {{total_sum}}:
            decimal totalAmount = totalSumWithNds;   // теперь с НДС!

            // 3. Загрузка шаблона
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "TTN.xlsx");
            if (!System.IO.File.Exists(templatePath))
                return Content($"Файл шаблона не найден: {templatePath}");

            Workbook workbook = new Workbook();
            workbook.LoadFromFile(templatePath);
            Worksheet sheet = workbook.Worksheets[0];

            // 4. ФИО водителя – Фамилия И.О.
            string driverFio = "";
            if (водитель != null)
            {
                string lastName = (водитель.фамилия ?? "").Trim();
                string firstName = (водитель.имя ?? "").Trim();
                string middleName = (водитель.отчество ?? "").Trim();

                string firstNameInitial = !string.IsNullOrWhiteSpace(firstName) ? firstName.Substring(0, 1) + "." : "";
                string middleNameInitial = !string.IsNullOrWhiteSpace(middleName) ? middleName.Substring(0, 1) + "." : "";

                driverFio = $"{lastName} {firstNameInitial}{middleNameInitial}".Trim();
                while (driverFio.Contains("  ")) driverFio = driverFio.Replace("  ", " ");
            }

            string formattedDate = "'" + документ.дата_создания.ToString("dd.MM.yyyy");
            string regNumber = (string.IsNullOrEmpty(транспорт?.регистрационный_номер) ? "" : "'" + транспорт.регистрационный_номер);
            string mark = транспорт?.Марка_Транспорта?.наименование_марки ?? "";
            var cultureForExcel = System.Globalization.CultureInfo.InvariantCulture;

            // 5. Основные замены (кроме строки 37)
            var mainReplacements = new Dictionary<string, string>
{
    { "{sender}", (грузоотправитель?.название ?? "") + ", " + (грузоотправитель?.адрес ?? "") },
    { "{sender_unp}", грузоотправитель?.унп ?? "" },
    { "{receiver}", (грузополучатель?.название ?? "") + ", " + (грузополучатель?.адрес ?? "") },
    { "{receiver_unp}", грузополучатель?.унп ?? "" },
    { "{date}", formattedDate },
    { "{mark}", mark },
    { "{reg_number}", regNumber },
    { "{driver_fio}", driverFio },
    { "{loading_point}", пунктПогрузки?.наименование ?? "" },
    { "{unloading_point}", пунктРазгрузки?.наименование ?? "" },
    { "{otpusk}", документ.отпуск_разрешил ?? "" },
    { "{sdal}", документ.сдал_грузоотправитель ?? "" },
    { "{total_c}", totalQuantity.ToString("F0", cultureForExcel) },
    { "{total_sum}", totalCost.ToString("F2", cultureForExcel).Replace(".", ",") },
    { "{total_sn}", totalNds.ToString("F2", cultureForExcel).Replace(".", ",") },
    { "{total_swn}", totalSumWithNds.ToString("F2", cultureForExcel).Replace(".", ",") },
    { "{total_weight}", totalWeight.ToString("F3", cultureForExcel).Replace(".", ",") },
    { "{total_sn_hand}", NumToTextHelper.SumInWords(totalNds) },
    { "{total_swn_hand}", NumToTextHelper.SumInWords(totalSumWithNds) },
    { "{total_w_hand}", NumToTextHelper.WeightInWords(totalWeight) },
    { "{total_w}", totalWeight.ToString("F3", cultureForExcel).Replace(".", ",") }
};

            foreach (var cell in sheet.AllocatedRange)
            {
                if (cell.Row == 37) continue;    // не трогаем строку-шаблон товаров
                if (cell.Value != null)
                {
                    string cellValue = cell.Value.ToString();
                    bool changed = false;
                    foreach (var rep in mainReplacements)
                    {
                        if (cellValue.Contains(rep.Key))
                        {
                            cellValue = cellValue.Replace(rep.Key, rep.Value);
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        cell.Value = cellValue;
                        cell.Style.NumberFormat = "@";
                    }
                }
            }

            // ======== 6. ТОВАРНЫЙ РАЗДЕЛ ========
            const int templateRow = 37;

            if (sheet.Range[templateRow, 1].Value == null ||
                !sheet.Range[templateRow, 1].Value.ToString().Contains("{good_name}"))
                return Content($"Строка шаблона товаров не найдена в строке {templateRow}");

            // 6.1 Запоминаем объединённые диапазоны в строке 37 и сортируем их
            var merges = new List<(int startCol, int endCol)>();
            foreach (var mergedRange in sheet.MergedCells)
            {
                if (mergedRange.Row == templateRow)
                {
                    int s = mergedRange.Column;
                    int e = s + mergedRange.ColumnCount - 1;
                    if (!merges.Any(m => m.startCol == s))
                        merges.Add((s, e));
                }
            }
            merges.Sort((a, b) => a.startCol.CompareTo(b.startCol));

            // 6.2 Массив плейсхолдеров
            string[] placeholders = {
        "{good_name}", "{ed}", "{count}", "{cost}", "{sum}",
        "{nds}", "{sum_nds}", "{sum_w_nds}", "", "{weight}", ""
    };

            // 6.3 Вставка недостающих строк (если товаров > 1)
            int rowsToInsert = позиции.Count - 1;
            if (rowsToInsert > 0)
            {
                sheet.InsertRow(templateRow + 1, rowsToInsert);
                for (int i = 0; i < rowsToInsert; i++)
                {
                    int newRow = templateRow + 1 + i;

                    for (int col = 1; col <= 11; col++)
                    {
                        var source = sheet.Range[templateRow, col];
                        var dest = sheet.Range[newRow, col];
                        dest.Value = source.Value;
                        dest.Style = source.Style;
                    }

                    foreach (var (s, e) in merges)
                        sheet.Range[newRow, s, newRow, e].Merge();
                }
            }

            // 6.4 Заполнение ВСЕХ строк реальными данными
            for (int i = 0; i < позиции.Count; i++)
            {
                int currentRow = templateRow + i;
                var pos = позиции[i];

                // Расставляем ключи для новых строк (кроме шаблонной)
                if (i > 0)
                {
                    for (int idx = 0; idx < merges.Count; idx++)
                    {
                        var (startCol, endCol) = merges[idx];
                        sheet.Range[currentRow, startCol, currentRow, endCol].Value = placeholders[idx];
                    }
                }

                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);

                // Ставка НДС – с двумя знаками после запятой (как 20,00 или 18,33)
                decimal rawNdsRate = pos.ставка_ндс ?? 0;
                string ndsRateDisplay = rawNdsRate.ToString("0.00", cultureForExcel).Replace(".", ",");

                for (int idx = 0; idx < merges.Count; idx++)
                {
                    var (startCol, endCol) = merges[idx];
                    var mergedRange = sheet.Range[currentRow, startCol, currentRow, endCol];
                    if (mergedRange.Value == null) continue;

                    string val = mergedRange.Value.ToString();

                    switch (idx)
                    {
                        case 0: // Наименование
                            val = val.Replace("{good_name}", pos.Товар?.наименование ?? "");
                            break;
                        case 1: // Единицы измерения
                            val = val.Replace("{ed}", pos.Товар?.единицы_измерения ?? "");
                            break;
                        case 2: // Количество
                            val = val.Replace("{count}", qty.ToString("0.000", cultureForExcel).Replace(".", ","));
                            break;
                        case 3: // Цена
                            val = val.Replace("{cost}", pos.цена_за_единицу.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 4: // Стоимость без НДС
                            val = val.Replace("{sum}", afterDiscount.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 5: // Ставка НДС (дробная)
                            val = val.Replace("{nds}", ndsRateDisplay);
                            break;
                        case 6: // Сумма НДС
                            val = val.Replace("{sum_nds}", vat.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 7: // Стоимость с НДС
                            val = val.Replace("{sum_w_nds}", (afterDiscount + vat).ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 9: // Масса
                            val = val.Replace("{weight}", (pos.масса_груза ?? 0).ToString("0.000", cultureForExcel).Replace(".", ","));
                            break;
                            // case 8 и case 10 — пустые, без замен
                    }

                    mergedRange.Value = val;
                }

                // Применяем текстовый формат и перенос для всех строк, включая первую
                foreach (var (startCol, endCol) in merges)
                {
                    var rng = sheet.Range[currentRow, startCol, currentRow, endCol];
                    rng.Style.NumberFormat = "@";
                    rng.Style.WrapText = true;
                }
                sheet.AutoFitRow(currentRow);

                // Дополнительное оформление только для новых строк (границы, шрифт, выравнивание)
                if (i > 0)
                {
                    for (int idx = 0; idx < merges.Count; idx++)
                    {
                        var (startCol, endCol) = merges[idx];
                        var rng = sheet.Range[currentRow, startCol, currentRow, endCol];
                        rng.Style.Font.Size = 9;
                        rng.Style.HorizontalAlignment = (idx == 0) ? HorizontalAlignType.Left : HorizontalAlignType.Center;

                        rng.Borders[BordersLineType.EdgeLeft].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeRight].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeTop].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeBottom].LineStyle = LineStyleType.Thin;
                        rng.Borders.Color = System.Drawing.Color.Black;
                    }
                }
            }

            if (позиции.Count == 0)
                sheet.DeleteRow(templateRow);

            // Отключаем автомасштабирование, чтобы документ не сжимался по ширине
            sheet.PageSetup.FitToPagesWide = 0;   // не подгонять под ширину страницы
            sheet.PageSetup.FitToPagesTall = 1;   // не подгонять под высоту (можно оставить 1, если нужно)        // масштаб 100%

            using (var stream = new MemoryStream())
            {
                workbook.SaveToStream(stream, Spire.Xls.FileFormat.PDF);
                byte[] pdfBytes = stream.ToArray();

                HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                HttpContext.Response.Headers["Pragma"] = "no-cache";
                HttpContext.Response.Headers["Expires"] = "0";
                return File(pdfBytes, "application/pdf");
            }
        }

        private async Task<IActionResult> GeneratePrintPdf(Documents документ)
        {
            var позиции = await _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == документ.ид_документа)
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
                return Content("Шаблон не найден: " + templatePath);

            var htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            var goodsHtml = new StringBuilder();
            decimal totalQuantity = 0, totalCost = 0, totalVat = 0, totalWeight = 0;
            int totalPackages = 0;

            foreach (var pos in позиции)
            {
                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);
                totalQuantity += qty;
                totalCost += afterDiscount;
                totalVat += vat;
                totalWeight += pos.масса_груза ?? 0;
                totalPackages += pos.грузовых_мест ?? 0;

                goodsHtml.AppendLine($@"
            <tr class=""goods-row"">
                <td>{pos.Товар?.наименование ?? ""}</td>
                <td class=""center"">{pos.Товар?.единицы_измерения ?? ""}</td>
                <td class=""right"">{pos.количество:F3}</td>
                <td class=""right"">{pos.цена_за_единицу:F2}</td>
                <td class=""right"">{pos.скидка ?? 0}</td>
                <td class=""center"">{pos.ставка_ндс ?? 0}</td>
                <td class=""right"">{afterDiscount:F2}</td>
                <td class=""right"">{vat:F2}</td>
                <td class=""right"">{afterDiscount + vat:F2}</td>
                <td class=""right"">{pos.грузовых_мест ?? 0}</td>
                <td class=""right"">{pos.масса_груза ?? 0:F3}</td>
                <td class=""right"">{pos.примечание ?? ""}</td>
            </tr>");
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
                .Replace("{{ПунктПогрузки}}", пунктПогрузки?.наименование ?? "")
                .Replace("{{ПунктРазгрузки}}", пунктРазгрузки?.наименование ?? "")
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
                .Replace("{{ОтпускРазрешил}}", документ.отпуск_разрешил ?? "")
                .Replace("{{СдалГрузоотправитель}}", документ.сдал_грузоотправитель ?? "")
                .Replace("{{ТоварПринял}}", "").Replace("{{НомерПломбы}}", "")
                .Replace("{{Доверенность}}", "").Replace("{{ДатаДоверенности}}", "")
                .Replace("{{Расстояние}}", "").Replace("{{ОсновнойТариф}}", "")
                .Replace("{{КОплате}}", "");

            var converter = new HtmlToPdf();
            var pdf = converter.ConvertHtmlString(html);
            HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            HttpContext.Response.Headers["Pragma"] = "no-cache";
            HttpContext.Response.Headers["Expires"] = "0";
            return File(pdf.Save(), "application/pdf");
        }

        // Добавьте в начало файла, если ещё нет:
        // using Spire.Doc;

        // Не забудьте добавить using в начало файла:
        // using Spire.Doc;
        // using Spire.Doc.Documents;
        // using Spire.Doc.Tables;

        // using Spire.Doc;
        // using Spire.Doc.Documents;
        // using Spire.Doc.Tables;
        // using Spire.Pdf;

        private async Task<IActionResult> GenerateCmrPdf(Documents документ)
        {
            // 1. Получение позиций
            var позиции = await _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == документ.ид_документа)
                .ToListAsync();

            var грузоотправитель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
            var грузополучатель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
            var перевозчик = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_перевозчика);
            var транспорт = await _context.Транспорт.FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);
            if (транспорт != null)
                await _context.Entry(транспорт).Reference(t => t.Марка_Транспорта).LoadAsync();
            var пунктПогрузки = await _context.Пункт_Погрузки.FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
            var пунктРазгрузки = await _context.Пункт_Разгрузки.FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "CMR.docx");
            if (!System.IO.File.Exists(templatePath))
                return Content($"Файл шаблона не найден: {templatePath}");

            const int maxRows = 7;
            int totalChunks = (int)Math.Ceiling((double)позиции.Count / maxRows);
            List<MemoryStream> pdfStreams = new List<MemoryStream>();

            // 2. Расчёт итогов (с НДС, как в ТТН)
            decimal totalWeight = 0;
            decimal totalCost = 0;          // без НДС
            decimal totalNds = 0;
            decimal totalSumWithNds = 0;    // с НДС

            foreach (var pos in позиции)
            {
                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);

                totalCost += afterDiscount;
                totalNds += vat;
                totalSumWithNds += afterDiscount + vat;
                totalWeight += pos.масса_груза ?? 0;
            }

            // Выбираем, что показывать как общую сумму: с НДС
            decimal totalAmount = totalSumWithNds;

            for (int chunk = 0; chunk < totalChunks; chunk++)
            {
                var chunkPositions = позиции.Skip(chunk * maxRows).Take(maxRows).ToList();

                string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
                System.IO.File.Copy(templatePath, tempFile, true);

                try
                {
                    Document doc = new Document();
                    doc.LoadFromFile(tempFile);

                    int позицияИндекс = 0;
                    foreach (Section section in doc.Sections)
                    {
                        foreach (Table table in section.Tables)
                        {
                            foreach (TableRow row in table.Rows)
                            {
                                bool hasPlaceholders = false;
                                for (int c = 0; c < row.Cells.Count; c++)
                                {
                                    TableCell cell = row.Cells[c];
                                    foreach (Paragraph paragraph in cell.Paragraphs)
                                    {
                                        if (paragraph.Text.Contains("{{good_name}}") || paragraph.Text.Contains("{{weight}}"))
                                        {
                                            hasPlaceholders = true;
                                            break;
                                        }
                                    }
                                    if (hasPlaceholders) break;
                                }

                                if (hasPlaceholders)
                                {
                                    if (позицияИндекс < chunkPositions.Count)
                                    {
                                        var pos = chunkPositions[позицияИндекс];
                                        for (int c = 0; c < row.Cells.Count; c++)
                                        {
                                            TableCell cell = row.Cells[c];
                                            foreach (Paragraph paragraph in cell.Paragraphs)
                                            {
                                                paragraph.Text = paragraph.Text
                                                    .Replace("{{good_name}}", pos.Товар?.наименование ?? "")
                                                    .Replace("{{weight}}", (pos.масса_груза ?? 0).ToString("0.000", CultureInfo.InvariantCulture).Replace(".", ",") + " кг");

                                                foreach (object item in paragraph.ChildObjects)
                                                {
                                                    if (item is TextRange textRange)
                                                    {
                                                        if (textRange.CharacterFormat != null)
                                                            textRange.CharacterFormat.Bold = false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int c = 0; c < row.Cells.Count; c++)
                                        {
                                            TableCell cell = row.Cells[c];
                                            foreach (Paragraph paragraph in cell.Paragraphs)
                                            {
                                                paragraph.Text = paragraph.Text
                                                    .Replace("{{good_name}}", "")
                                                    .Replace("{{weight}}", "");

                                                foreach (object item in paragraph.ChildObjects)
                                                {
                                                    if (item is TextRange textRange)
                                                    {
                                                        if (textRange.CharacterFormat != null)
                                                            textRange.CharacterFormat.Bold = false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    позицияИндекс++;
                                }
                            }
                        }
                    }

                    var replacements = new Dictionary<string, string>
            {
                { "{{sender}}", (грузоотправитель?.название ?? "") + ", " + (грузоотправитель?.адрес ?? "") },
                { "{{receiver}}", (грузополучатель?.название ?? "") + ", " + (грузополучатель?.адрес ?? "") },
                { "{{transporter}}", перевозчик?.название ?? "" },
                { "{{unloading_point}}", пунктРазгрузки?.наименование ?? "" },
                { "{{loading_point}}", пунктПогрузки?.наименование ?? "" },
                { "{{date}}", документ.дата_создания.ToString("dd.MM.yyyy") },
                { "{{reg_number}}", транспорт?.регистрационный_номер ?? "" },
                { "{{mark}}", транспорт?.Марка_Транспорта?.наименование_марки ?? "" },
                { "{{doc_number}}", документ?.номер_документа ?? "" },
                { "{{total_weight}}", totalWeight.ToString("0.000", CultureInfo.InvariantCulture).Replace(".", ",") + " кг" },
                { "{{total_sum}}", totalAmount.ToString("0.00", CultureInfo.InvariantCulture).Replace(".", ",") }
            };

                    foreach (var kv in replacements)
                        doc.Replace(kv.Key, kv.Value, false, true);

                    MemoryStream pdfStream = new MemoryStream();
                    doc.SaveToStream(pdfStream, Spire.Doc.FileFormat.PDF);
                    pdfStream.Position = 0;
                    pdfStreams.Add(pdfStream);
                }
                finally
                {
                    if (System.IO.File.Exists(tempFile))
                        System.IO.File.Delete(tempFile);
                }
            }

            // Объединение PDF (без смещения)
            if (pdfStreams.Count == 1)
            {
                return File(pdfStreams[0].ToArray(), "application/pdf");
            }
            else
            {
                MemoryStream[] streams = pdfStreams.ToArray();
                using (MemoryStream mergedPdf = new MemoryStream())
                {
                    Spire.Pdf.PdfDocument.MergeFiles(streams, mergedPdf);
                    foreach (var s in pdfStreams) s.Dispose();
                    mergedPdf.Position = 0;

                    HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    HttpContext.Response.Headers["Pragma"] = "no-cache";
                    HttpContext.Response.Headers["Expires"] = "0";
                    return File(mergedPdf.ToArray(), "application/pdf");
                }
            }
        }

        private async Task<IActionResult> GenerateTnPdf(Documents документ)
        {
            // 1. Получение позиций и связанных данных
            var позиции = await _context.Позиции
                .Include(p => p.Товар)
                .Where(p => p.ид_документа == документ.ид_документа)
                .ToListAsync();

            var грузоотправитель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
            var грузополучатель = await _context.Организации.FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
            var водитель = await _context.Водители.FirstOrDefaultAsync(d => d.ид_водителя == документ.ид_водителя);
            var транспорт = await _context.Транспорт.FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);
            if (транспорт != null)
                await _context.Entry(транспорт).Reference(t => t.Марка_Транспорта).LoadAsync();
            var пунктПогрузки = await _context.Пункт_Погрузки.FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
            var пунктРазгрузки = await _context.Пункт_Разгрузки.FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);

            // 2. Подсчёт итогов
            decimal totalQuantity = 0, totalCost = 0, totalNds = 0, totalSumWithNds = 0, totalWeight = 0;
            int totalPackages = 0;
            foreach (var pos in позиции)
            {
                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);
                totalQuantity += qty;
                totalCost += afterDiscount;
                totalNds += vat;
                totalSumWithNds += afterDiscount + vat;
                totalWeight += pos.масса_груза ?? 0;
                totalPackages += pos.грузовых_мест ?? 0;
            }

            // 3. Загрузка шаблона
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "TN.xlsx");
            if (!System.IO.File.Exists(templatePath))
                return Content($"Файл шаблона не найден: {templatePath}");

            Workbook workbook = new Workbook();
            workbook.LoadFromFile(templatePath);
            Worksheet sheet = workbook.Worksheets[0];

            // 4. ФИО водителя – Фамилия И.О.
            string driverFio = "";
            if (водитель != null)
            {
                string lastName = (водитель.фамилия ?? "").Trim();
                string firstName = (водитель.имя ?? "").Trim();
                string middleName = (водитель.отчество ?? "").Trim();

                string firstNameInitial = !string.IsNullOrWhiteSpace(firstName) ? firstName.Substring(0, 1) + "." : "";
                string middleNameInitial = !string.IsNullOrWhiteSpace(middleName) ? middleName.Substring(0, 1) + "." : "";

                driverFio = $"{lastName} {firstNameInitial}{middleNameInitial}".Trim();
                while (driverFio.Contains("  ")) driverFio = driverFio.Replace("  ", " ");
            }

            string formattedDate = "'" + документ.дата_создания.ToString("dd.MM.yyyy");
            string regNumber = (string.IsNullOrEmpty(транспорт?.регистрационный_номер) ? "" : "'" + транспорт.регистрационный_номер);
            string mark = транспорт?.Марка_Транспорта?.наименование_марки ?? "";
            var cultureForExcel = System.Globalization.CultureInfo.InvariantCulture;

            // 5. Основные замены (кроме строки 30)
            var mainReplacements = new Dictionary<string, string>
    {
        { "{sender}", (грузоотправитель?.название ?? "") + ", " + (грузоотправитель?.адрес ?? "") },
         { "{sender_unp}", грузоотправитель?.унп ?? "" },
        { "{receiver}", (грузополучатель?.название ?? "") + ", " + (грузополучатель?.адрес ?? "") },
         { "{receiver_unp}", грузополучатель?.унп ?? "" },
        { "{date}", formattedDate },
        { "{mark}", mark },
        { "{reg_number}", regNumber },
        { "{driver_fio}", driverFio },
        { "{loading_point}", пунктПогрузки?.наименование ?? "" },
        { "{unloading_point}", пунктРазгрузки?.наименование ?? "" },
        { "{otpusk}", документ.отпуск_разрешил ?? "" },
        { "{sdal}", документ.сдал_грузоотправитель ?? "" },
        { "{total_c}", totalQuantity.ToString("F0", cultureForExcel) },
        { "{total_sum}", totalCost.ToString("F2", cultureForExcel).Replace(".", ",") },
        { "{total_sn}", totalNds.ToString("F2", cultureForExcel).Replace(".", ",") },
        { "{total_swn}", totalSumWithNds.ToString("F2", cultureForExcel).Replace(".", ",") },
        { "{total_weight}", totalWeight.ToString("F3", cultureForExcel).Replace(".", ",") },
        { "{total_sn_hand}", NumToTextHelper.SumInWords(totalNds) },
        { "{total_swn_hand}", NumToTextHelper.SumInWords(totalSumWithNds) },
        { "{total_w_hand}", NumToTextHelper.WeightInWords(totalWeight) },
        { "{total_w}", totalWeight.ToString("F3", cultureForExcel).Replace(".", ",") }
    };

            foreach (var cell in sheet.AllocatedRange)
            {
                if (cell.Row == 30) continue;    // пропускаем строку-шаблон товаров
                if (cell.Value != null)
                {
                    string cellValue = cell.Value.ToString();
                    bool changed = false;
                    foreach (var rep in mainReplacements)
                    {
                        if (cellValue.Contains(rep.Key))
                        {
                            cellValue = cellValue.Replace(rep.Key, rep.Value);
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        cell.Value = cellValue;
                        cell.Style.NumberFormat = "@";
                    }
                }
            }

            // ======== 6. ТОВАРНЫЙ РАЗДЕЛ ========
            const int templateRow = 30;

            if (sheet.Range[templateRow, 1].Value == null ||
                !sheet.Range[templateRow, 1].Value.ToString().Contains("{good_name}"))
                return Content($"Строка шаблона товаров не найдена в строке {templateRow}");

            // 6.1 Запоминаем объединённые диапазоны в строке 30 и сортируем их
            var merges = new List<(int startCol, int endCol)>();
            foreach (var mergedRange in sheet.MergedCells)
            {
                if (mergedRange.Row == templateRow)
                {
                    int s = mergedRange.Column;
                    int e = s + mergedRange.ColumnCount - 1;
                    if (!merges.Any(m => m.startCol == s))
                        merges.Add((s, e));
                }
            }
            merges.Sort((a, b) => a.startCol.CompareTo(b.startCol));

            // 6.2 Массив плейсхолдеров
            string[] placeholders = {
        "{good_name}", "{ed}", "{count}", "{cost}", "{sum}",
        "{nds}", "{sum_nds}", "{sum_w_nds}", "", "{weight}", ""
    };

            // 6.3 Вставка недостающих строк (если товаров > 1)
            int rowsToInsert = позиции.Count - 1;
            if (rowsToInsert > 0)
            {
                sheet.InsertRow(templateRow + 1, rowsToInsert);
                for (int i = 0; i < rowsToInsert; i++)
                {
                    int newRow = templateRow + 1 + i;

                    for (int col = 1; col <= 11; col++)
                    {
                        var source = sheet.Range[templateRow, col];
                        var dest = sheet.Range[newRow, col];
                        dest.Value = source.Value;
                        dest.Style = source.Style;
                    }

                    foreach (var (s, e) in merges)
                        sheet.Range[newRow, s, newRow, e].Merge();
                }
            }

            // 6.4 Заполнение ВСЕХ строк реальными данными
            for (int i = 0; i < позиции.Count; i++)
            {
                int currentRow = templateRow + i;
                var pos = позиции[i];

                if (i > 0)
                {
                    for (int idx = 0; idx < merges.Count; idx++)
                    {
                        var (startCol, endCol) = merges[idx];
                        sheet.Range[currentRow, startCol, currentRow, endCol].Value = placeholders[idx];
                    }
                }

                decimal qty = (decimal)pos.количество;
                decimal cost = pos.цена_за_единицу * qty;
                decimal discount = cost * ((pos.скидка ?? 0) / 100);
                decimal afterDiscount = cost - discount;
                decimal vat = afterDiscount * ((pos.ставка_ндс ?? 0) / 100);

                decimal rawNdsRate = pos.ставка_ндс ?? 0;
                string ndsRateDisplay = rawNdsRate.ToString("0.00", cultureForExcel).Replace(".", ",");

                for (int idx = 0; idx < merges.Count; idx++)
                {
                    var (startCol, endCol) = merges[idx];
                    var mergedRange = sheet.Range[currentRow, startCol, currentRow, endCol];
                    if (mergedRange.Value == null) continue;

                    string val = mergedRange.Value.ToString();

                    switch (idx)
                    {
                        case 0: // Наименование
                            val = val.Replace("{good_name}", pos.Товар?.наименование ?? "");
                            break;
                        case 1: // Единицы измерения
                            val = val.Replace("{ed}", pos.Товар?.единицы_измерения ?? "");
                            break;
                        case 2: // Количество
                            val = val.Replace("{count}", qty.ToString("0.000", cultureForExcel).Replace(".", ","));
                            break;
                        case 3: // Цена
                            val = val.Replace("{cost}", pos.цена_за_единицу.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 4: // Стоимость без НДС
                            val = val.Replace("{sum}", afterDiscount.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 5: // Ставка НДС
                            val = val.Replace("{nds}", ndsRateDisplay);
                            break;
                        case 6: // Сумма НДС
                            val = val.Replace("{sum_nds}", vat.ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 7: // Стоимость с НДС
                            val = val.Replace("{sum_w_nds}", (afterDiscount + vat).ToString("0.00", cultureForExcel).Replace(".", ","));
                            break;
                        case 9: // Масса
                            val = val.Replace("{weight}", (pos.масса_груза ?? 0).ToString("0.000", cultureForExcel).Replace(".", ","));
                            break;
                    }

                    mergedRange.Value = val;
                }

                // Применяем текстовый формат и перенос для всех строк, включая первую
                foreach (var (startCol, endCol) in merges)
                {
                    var rng = sheet.Range[currentRow, startCol, currentRow, endCol];
                    rng.Style.NumberFormat = "@";
                    rng.Style.WrapText = true;
                }
                sheet.AutoFitRow(currentRow);

                // Дополнительное оформление только для новых строк
                if (i > 0)
                {
                    for (int idx = 0; idx < merges.Count; idx++)
                    {
                        var (startCol, endCol) = merges[idx];
                        var rng = sheet.Range[currentRow, startCol, currentRow, endCol];
                        rng.Style.Font.Size = 9;
                        rng.Style.HorizontalAlignment = (idx == 0) ? HorizontalAlignType.Left : HorizontalAlignType.Center;

                        rng.Borders[BordersLineType.EdgeLeft].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeRight].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeTop].LineStyle = LineStyleType.Thin;
                        rng.Borders[BordersLineType.EdgeBottom].LineStyle = LineStyleType.Thin;
                        rng.Borders.Color = System.Drawing.Color.Black;
                    }
                }
            }

            if (позиции.Count == 0)
                sheet.DeleteRow(templateRow);

            // Фиксируем масштаб
            sheet.PageSetup.FitToPagesWide = 0;
            sheet.PageSetup.FitToPagesTall = 1;

            using (var stream = new MemoryStream())
            {
                workbook.SaveToStream(stream, Spire.Xls.FileFormat.PDF);
                byte[] pdfBytes = stream.ToArray();

                HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                HttpContext.Response.Headers["Pragma"] = "no-cache";
                HttpContext.Response.Headers["Expires"] = "0";
                return File(pdfBytes, "application/pdf");
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

        // Страница управления маршрутами (для админа)
        [HttpGet]
        public IActionResult ManageRoutes()
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

            var routes = _context.Маршруты
                .Include(r => r.Водитель)
                .Include(r => r.Транспорт)
                .Include(r => r.ТочкиМаршрута)
                    .ThenInclude(t => t.ПунктПогрузки)
                .Include(r => r.ТочкиМаршрута)
                    .ThenInclude(t => t.ПунктРазгрузки)
                .Where(r => r.ид_организации == userOrgId)
                .ToList();

            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
            ViewBag.Organizations = _context.Организации.Where(o => o.ид_владельца == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();

            return View(routes);
        }

        // Создание накладной по маршруту (для пользователя)
        [HttpGet]
        public IActionResult CreateDocumentFromRoute(int routeId)
        {
            var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
            int userOrgId = int.Parse(userOrgIdStr ?? "1");

            var route = _context.Маршруты
                .Include(r => r.Водитель)
                .Include(r => r.Транспорт)
                .Include(r => r.Перевозчик)
                .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                    .ThenInclude(t => t.ПунктПогрузки)
                .Include(r => r.ТочкиМаршрута)
                    .ThenInclude(t => t.ПунктРазгрузки)
                .FirstOrDefault(r => r.ид_маршрута == routeId && r.ид_организации == userOrgId);

            if (route == null) return NotFound();

            // Определяем текущую точку (первую незавершенную)
            var completedDocCount = _context.Документы
                .Count(d => d.ид_маршрута == routeId);

            var currentPoint = route.ТочкиМаршрута
                .OrderBy(t => t.порядковый_номер)
                .Skip(completedDocCount)
                .FirstOrDefault();

            if (currentPoint == null)
            {
                TempData["Success"] = "Маршрут завершен!";
                return RedirectToAction("Index");
            }

            // Генерируем автоинкрементный номер
            var lastDocNumber = _context.Документы
                .Where(d => d.ид_грузоотправителя == userOrgId)
                .OrderByDescending(d => d.ид_документа)
                .Select(d => d.номер_документа)
                .FirstOrDefault();

            string nextNumber = "000001";
            if (!string.IsNullOrEmpty(lastDocNumber) && int.TryParse(lastDocNumber, out int lastNum))
            {
                nextNumber = (lastNum + 1).ToString("D6");
            }

            var model = new RouteDocumentViewModel
            {
                ид_маршрута = routeId,
                название_маршрута = route.название,
                текущая_точка_индекс = completedDocCount,
                всего_точек = route.ТочкиМаршрута.Count,
                текущая_точка = currentPoint,
                все_точки = route.ТочкиМаршрута.ToList(),
                ид_водителя = route.ид_водителя ?? 0,
                ид_транспорта = route.ид_транспорта ?? 0,
                ид_перевозчика = route.ид_перевозчика ?? 0,
                ид_грузоотправителя = userOrgId,
                номер_документа = nextNumber,
                дата_создания = DateTime.Now,
                Товары = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList(),
                ПунктыПогрузки = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList(),
                ПунктыРазгрузки = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList(),
                ТипыДокументов = _context.Типы_Документов.ToList()
            };

            // Передаем товары в JSON для JavaScript
            ViewBag.GoodsJson = JsonSerializer.Serialize(
                model.Товары.Select(g => new {
                    ид_товара = g.ид_товара,
                    наименование = g.наименование,
                    единицы_измерения = g.единицы_измерения
                })
            );

            return View(model);
        }

        // Сохранение накладной и переход к следующей точке
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDocumentAndNext(RouteDocumentViewModel model, string positionsData)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                var userId = int.Parse(userIdStr ?? "1");
                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int userOrgId = int.Parse(userOrgIdStr ?? "1");

                var document = new Documents
                {
                    номер_документа = model.номер_документа,
                    дата_создания = model.дата_создания,
                    ид_типа = model.ид_типа,
                    ид_грузоотправителя = model.ид_грузоотправителя,
                    ид_перевозчика = model.ид_перевозчика,
                    ид_получателя = model.ид_грузоотправителя,
                    ид_водителя = model.ид_водителя,
                    ид_транспорта = model.ид_транспорта,
                    ид_пункта_погрузки = model.текущая_точка?.ид_пункта_погрузки,
                    ид_пункта_разгрузки = model.текущая_точка?.ид_пункта_разгрузки,
                    ид_пользователя = userId
                };


                document.ид_маршрута = model.ид_маршрута;

                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                _context.Документы.Add(document);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

                // Сохраняем позиции
                if (!string.IsNullOrEmpty(positionsData))
                {
                    var positions = JsonSerializer.Deserialize<List<PositionViewModel>>(positionsData);
                    if (positions != null)
                    {
                        foreach (var pos in positions)
                        {
                            if (pos.goodsId <= 0 || pos.quantity <= 0 || pos.price <= 0) continue;

                            var position = new Positions
                            {
                                ид_документа = document.ид_документа,
                                ид_товара = pos.goodsId,
                                количество = pos.quantity,
                                цена_за_единицу = pos.price,
                                ставка_ндс = pos.vatRate,
                                скидка = pos.discount,
                                масса_груза = pos.weight,
                                сумма_ндс = pos.price * (decimal)pos.quantity * (pos.vatRate / 100),
                                стоимость_с_ндс = pos.price * (decimal)pos.quantity * (1 + pos.vatRate / 100)
                            };
                            _context.Позиции.Add(position);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Проверяем, есть ли следующая точка
                var nextPoint = _context.Точки_Маршрута
                    .Where(t => t.ид_маршрута == model.ид_маршрута
                             && t.порядковый_номер > (model.текущая_точка_индекс + 1))
                    .OrderBy(t => t.порядковый_номер)
                    .FirstOrDefault();

                if (nextPoint != null)
                {
                    return RedirectToAction("CreateDocumentFromRoute", new { routeId = model.ид_маршрута });
                }

                TempData["Success"] = "Маршрут завершен!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка: {ex.Message}");
                // Перезагружаем данные для формы
                return RedirectToAction("CreateDocumentFromRoute", new { routeId = model.ид_маршрута });
            }
        }
        
        
    }
}