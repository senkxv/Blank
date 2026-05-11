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
            ViewBag.Organizations = _context.Организации.Where(o => o.ид_владельца == userOrgId).ToList();
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
            ViewBag.UserOrgId = userOrgId;

            // Загружаем доступные маршруты
            ViewBag.AvailableRoutes = _context.Маршруты
                .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                .Where(r => r.ид_организации == userOrgId && r.статус == "активен")
                .ToList();

            // Если для текущей организации нет маршрутов, показываем все активные
            if (ViewBag.AvailableRoutes.Count == 0)
            {
                ViewBag.AvailableRoutes = _context.Маршруты
                    .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                    .Where(r => r.статус == "активен")
                    .ToList();
            }

            // Если выбран маршрут — предзаполняем данные
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
                    // Используем переданный индекс или считаем по документам
                    int completedDocs;
                    if (currentPointIndex.HasValue)
                    {
                        completedDocs = currentPointIndex.Value;
                    }
                    else
                    {
                        completedDocs = _context.Документы.Count(d => d.ид_маршрута == routeId);
                    }

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

                    // Грузоотправитель и грузополучатель из текущей точки
                    ViewBag.DefaultSenderId = currentPoint?.ид_грузоотправителя;
                    ViewBag.DefaultReceiverId = currentPoint?.ид_грузополучателя;

                    // Генерируем следующий номер документа
                    var lastDoc = _context.Документы
                        .OrderByDescending(d => d.ид_документа)
                        .Select(d => d.номер_документа)
                        .FirstOrDefault();

                    string nextNumber = "000001";
                    if (!string.IsNullOrEmpty(lastDoc) && int.TryParse(lastDoc, out int lastNum))
                    {
                        nextNumber = (lastNum + 1).ToString("D6");
                    }

                    ViewBag.NextDocumentNumber = nextNumber;
                }
            }

            // Передаем товары в JSON для JS
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

                // Если есть маршрут — привязываем документ
                if (routeId.HasValue)
                {
                    document.ид_маршрута = routeId;
                }

                // Проверка уникальности номера документа
                var existingDoc = await _context.Документы
                    .FirstOrDefaultAsync(d => d.номер_документа == document.номер_документа
                                           && d.ид_грузоотправителя == userOrgId);
                if (existingDoc != null)
                {
                    ModelState.AddModelError("номер_документа", "Документ с таким номером уже существует в вашей организации");

                    // Перезагружаем ViewBag для повторного отображения формы
                    ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
                    ViewBag.Organizations = _context.Организации.Where(o => o.ид_владельца == userOrgId).ToList();
                    ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
                    ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
                    ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
                    ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
                    ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
                    ViewBag.UserOrgId = userOrgId;

                    // Маршруты
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


                    // Передаем товары в JSON для JS
                    ViewBag.GoodsJson = JsonSerializer.Serialize(
                        _context.Товары.Where(g => g.ид_организации == userOrgId)
                            .Select(g => new { g.ид_товара, g.наименование, g.единицы_измерения })
                            .ToList()
                    );

                    return View(document);
                }

                // Устанавливаем дату, если не указана
                if (document.дата_создания == default)
                {
                    document.дата_создания = DateTime.Now;
                }

                // Сохраняем документ
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                _context.Документы.Add(document);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

                int documentId = document.ид_документа;
                int positionsSavedCount = 0;

                // Сохраняем позиции
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

                // ✅ Если нажата кнопка "Следующая точка" — редирект на ту же страницу с маршрутом

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
                // Собираем полную информацию об ошибке
                var fullError = ex.Message;
                if (ex.InnerException != null)
                {
                    fullError += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        fullError += " | Nested: " + ex.InnerException.InnerException.Message;
                    }
                }

                ModelState.AddModelError("", fullError);

                // Перезагружаем данные для повторного отображения формы
                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int? userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? null : int.Parse(userOrgIdStr);

                ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
                ViewBag.Organizations = _context.Организации.Where(o => o.ид_владельца == userOrgId).ToList();
                ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
                ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();
                ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
                ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
                ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
                ViewBag.UserOrgId = userOrgId;

                // Маршруты
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

                // Передаем товары в JSON для JS
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
                // Получаем список ID всех организаций пользователя
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

            // Показываем все организации пользователя + организацию из документа
            var orgs = _context.Организации
                .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                .ToList();
            var docOrg = _context.Организации.Find(document.ид_грузоотправителя);
            if (docOrg != null && !orgs.Any(o => o.ид_организации == docOrg.ид_организации))
            {
                orgs.Add(docOrg);
            }
            ViewBag.Organizations = orgs;

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

            // В конце метода, перед return View(document)
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

                    // Находим точку, соответствующую этому документу
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

                // Получаем список ID всех организаций пользователя
                var userOrgIds = _context.Организации
                    .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                    .Select(o => o.ид_организации)
                    .ToList();

                // Проверка доступа
                if (userOrgId.HasValue && originalDoc != null)
                {
                    if (!userOrgIds.Contains(originalDoc.ид_грузоотправителя)
                        && !userOrgIds.Contains(originalDoc.ид_перевозчика)
                        && !userOrgIds.Contains(originalDoc.ид_получателя))
                    {
                        return NotFound();
                    }

                    // Проверяем, что новая организация принадлежит пользователю
                    if (!userOrgIds.Contains(document.ид_грузоотправителя))
                    {
                        document.ид_грузоотправителя = originalDoc.ид_грузоотправителя;
                    }
                    if (!userOrgIds.Contains(document.ид_перевозчика))
                    {
                        document.ид_перевозчика = originalDoc.ид_перевозчика;
                    }
                    if (!userOrgIds.Contains(document.ид_получателя))
                    {
                        document.ид_получателя = originalDoc.ид_получателя;
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
                            {
                                continue;
                            }

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

            // БЛОК CATCH — с фильтрацией
            var userOrgIdStr2 = HttpContext.Session.GetString("UserOrgId");
            int? userOrgId2 = string.IsNullOrEmpty(userOrgIdStr2) ? null : int.Parse(userOrgIdStr2);

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();

            var orgs = _context.Организации
                .Where(o => o.ид_организации == userOrgId2 || o.ид_владельца == userOrgId2)
                .ToList();
            var docOrg = await _context.Организации.FindAsync(document.ид_грузоотправителя);
            if (docOrg != null && !orgs.Any(o => o.ид_организации == docOrg.ид_организации))
            {
                orgs.Add(docOrg);
            }
            ViewBag.Organizations = orgs;

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

            // Получаем список ID всех организаций, принадлежащих администратору
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

                // Лист: Марки транспорта
                var sheetMarks = package.Workbook.Worksheets.Add("МаркиТранспорта");
                sheetMarks.Cells[1, 1].Value = "ид_марки";
                sheetMarks.Cells[1, 2].Value = "наименование_марки";

                var marks = _context.Марка_Транспорта.ToList();
                row = 2;
                foreach (var mark in marks)
                {
                    sheetMarks.Cells[row, 1].Value = mark.ид_марки;
                    sheetMarks.Cells[row, 2].Value = mark.наименование_марки;
                    row++;
                }
                sheetMarks.Cells.AutoFitColumns();

                // Лист: Типы транспорта
                var sheetTransportTypesExport = package.Workbook.Worksheets.Add("ТипыТранспорта");
                sheetTransportTypesExport.Cells[1, 1].Value = "ид_типа_транспорта";
                sheetTransportTypesExport.Cells[1, 2].Value = "наименование_типа";

                var types = _context.Тип_Транспорта.ToList();
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
                                await _context.Database.ExecuteSqlRawAsync("SET SQL_SAFE_UPDATES = 0;");
                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");

                                // ========== 1. ОЧИЩАЕМ ТАБЛИЦЫ ==========
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Позиции;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Документы;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Пункт_Погрузки;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Пункт_Разгрузки;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Водители;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Транспорт;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Организации;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Товары;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Типы_Документов;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Марки_Транспорта;");
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Типы_Транспорта;");

                                // ========== МАППИНГИ ==========
                                Dictionary<int, int> orgIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> driverIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> transportIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> loadingPointIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> unloadingPointIdMapping = new Dictionary<int, int>();
                                Dictionary<int, int> goodsIdMapping = new Dictionary<int, int>();

                                // ========== 2. МАРКИ ТРАНСПОРТА ==========
                                var sheetMarks = package.Workbook.Worksheets["МаркиТранспорта"];
                                if (sheetMarks != null && sheetMarks.Dimension != null && sheetMarks.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetMarks.Dimension.Rows; row++)
                                    {
                                        var id = sheetMarks.Cells[row, 1]?.Value;
                                        var name = sheetMarks.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Марки_Транспорта (ид_марки, наименование_марки) VALUES ({0}, {1})",
                                            Convert.ToInt32(id), name);
                                    }
                                }

                                // ========== 3. ТИПЫ ТРАНСПОРТА ==========
                                var sheetTransportTypes = package.Workbook.Worksheets["ТипыТранспорта"];
                                if (sheetTransportTypes != null && sheetTransportTypes.Dimension != null && sheetTransportTypes.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetTransportTypes.Dimension.Rows; row++)
                                    {
                                        var id = sheetTransportTypes.Cells[row, 1]?.Value;
                                        var name = sheetTransportTypes.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Типы_Транспорта (ид_типа_транспорта, наименование_типа) VALUES ({0}, {1})",
                                            Convert.ToInt32(id), name);
                                    }
                                }

                                // ========== 4. ТИПЫ ДОКУМЕНТОВ ==========
                                var sheetTypes = package.Workbook.Worksheets["ТипыДокументов"];
                                if (sheetTypes != null && sheetTypes.Dimension != null && sheetTypes.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetTypes.Dimension.Rows; row++)
                                    {
                                        var id = sheetTypes.Cells[row, 1]?.Value;
                                        var shortName = sheetTypes.Cells[row, 2]?.Value?.ToString();
                                        var fullName = sheetTypes.Cells[row, 3]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(shortName)) continue;
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Типы_Документов (ид_типа, краткое_наименование, полное_наименование) VALUES ({0}, {1}, {2})",
                                            Convert.ToInt32(id), shortName, fullName ?? "");
                                    }
                                }

                                // ========== 5. ОРГАНИЗАЦИИ С МАППИНГОМ ==========
                                var sheetOrgs = package.Workbook.Worksheets["Организации"];
                                if (sheetOrgs != null && sheetOrgs.Dimension != null && sheetOrgs.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetOrgs.Dimension.Rows; row++)
                                    {
                                        var id = sheetOrgs.Cells[row, 1]?.Value;
                                        var name = sheetOrgs.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;

                                        int oldOrgId = Convert.ToInt32(id);
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Организации (название, унп, адрес, почта, ид_владельца) VALUES ({0}, {1}, {2}, {3}, {4})",
                                            name,
                                            sheetOrgs.Cells[row, 3]?.Value?.ToString() ?? "",
                                            sheetOrgs.Cells[row, 4]?.Value?.ToString() ?? "",
                                            sheetOrgs.Cells[row, 5]?.Value?.ToString() ?? "",
                                            ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            orgIdMapping[oldOrgId] = newId;
                                        }
                                    }
                                }

                                // ========== 6. ТОВАРЫ С МАППИНГОМ ==========
                                var sheetGoods = package.Workbook.Worksheets["Товары"];
                                if (sheetGoods != null && sheetGoods.Dimension != null && sheetGoods.Dimension.Rows > 1)
                                {
                                    var header2 = sheetGoods.Cells[1, 2]?.Value?.ToString() ?? "";
                                    bool hasCodeColumn = header2 == "код_товара";

                                    for (int row = 2; row <= sheetGoods.Dimension.Rows; row++)
                                    {
                                        var id = sheetGoods.Cells[row, 1]?.Value;
                                        if (id == null) continue;

                                        int oldGoodsId = Convert.ToInt32(id);
                                        string code, name, unit;
                                        if (hasCodeColumn)
                                        {
                                            code = sheetGoods.Cells[row, 2]?.Value?.ToString() ?? "";
                                            name = sheetGoods.Cells[row, 3]?.Value?.ToString() ?? "";
                                            unit = sheetGoods.Cells[row, 4]?.Value?.ToString() ?? "";
                                        }
                                        else
                                        {
                                            code = "";
                                            name = sheetGoods.Cells[row, 2]?.Value?.ToString() ?? "";
                                            unit = sheetGoods.Cells[row, 3]?.Value?.ToString() ?? "";
                                        }

                                        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code)) continue;

                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Товары (код_товара, наименование, единицы_измерения, ид_организации) VALUES ({0}, {1}, {2}, {3})",
                                            code, name, unit, ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            goodsIdMapping[oldGoodsId] = newId;
                                        }
                                    }
                                }

                                // ========== 7. ВОДИТЕЛИ С МАППИНГОМ ==========
                                var sheetDrivers = package.Workbook.Worksheets["Водители"];
                                if (sheetDrivers != null && sheetDrivers.Dimension != null && sheetDrivers.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetDrivers.Dimension.Rows; row++)
                                    {
                                        var id = sheetDrivers.Cells[row, 1]?.Value;
                                        var lastName = sheetDrivers.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(lastName)) continue;

                                        int oldDriverId = Convert.ToInt32(id);
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Водители (фамилия, имя, отчество, номер_лицензии, ид_организации) VALUES ({0}, {1}, {2}, {3}, {4})",
                                            lastName,
                                            sheetDrivers.Cells[row, 3]?.Value?.ToString() ?? "",
                                            sheetDrivers.Cells[row, 4]?.Value?.ToString() ?? "",
                                            sheetDrivers.Cells[row, 5]?.Value?.ToString() ?? "",
                                            ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            driverIdMapping[oldDriverId] = newId;
                                        }
                                    }
                                }

                                // ========== 8. ТРАНСПОРТ С МАППИНГОМ ==========
                                var sheetTransport = package.Workbook.Worksheets["Транспорт"];
                                if (sheetTransport != null && sheetTransport.Dimension != null && sheetTransport.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetTransport.Dimension.Rows; row++)
                                    {
                                        var id = sheetTransport.Cells[row, 1]?.Value;
                                        var regNumber = sheetTransport.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(regNumber)) continue;

                                        int oldTransportId = Convert.ToInt32(id);
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Транспорт (регистрационный_номер, ид_марки, ид_типа_транспорта, ид_организации) VALUES ({0}, {1}, {2}, {3})",
                                            regNumber,
                                            sheetTransport.Cells[row, 3]?.Value ?? 1,
                                            sheetTransport.Cells[row, 4]?.Value ?? 1,
                                            ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            transportIdMapping[oldTransportId] = newId;
                                        }
                                    }
                                }

                                // ========== 9. ПУНКТЫ ПОГРУЗКИ С МАППИНГОМ ==========
                                var sheetLoading = package.Workbook.Worksheets["ПунктыПогрузки"];
                                if (sheetLoading != null && sheetLoading.Dimension != null && sheetLoading.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetLoading.Dimension.Rows; row++)
                                    {
                                        var id = sheetLoading.Cells[row, 1]?.Value;
                                        var name = sheetLoading.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;

                                        int oldPointId = Convert.ToInt32(id);
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Пункт_Погрузки (наименование, адрес, ид_организации) VALUES ({0}, {1}, {2})",
                                            name,
                                            sheetLoading.Cells[row, 3]?.Value?.ToString() ?? "",
                                            ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            loadingPointIdMapping[oldPointId] = newId;
                                        }
                                    }
                                }

                                // ========== 10. ПУНКТЫ РАЗГРУЗКИ С МАППИНГОМ ==========
                                var sheetUnloading = package.Workbook.Worksheets["ПунктыРазгрузки"];
                                if (sheetUnloading != null && sheetUnloading.Dimension != null && sheetUnloading.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetUnloading.Dimension.Rows; row++)
                                    {
                                        var id = sheetUnloading.Cells[row, 1]?.Value;
                                        var name = sheetUnloading.Cells[row, 2]?.Value?.ToString();
                                        if (id == null || string.IsNullOrEmpty(name)) continue;

                                        int oldPointId = Convert.ToInt32(id);
                                        await _context.Database.ExecuteSqlRawAsync(
                                            "INSERT INTO Пункт_Разгрузки (наименование, адрес, ид_организации) VALUES ({0}, {1}, {2})",
                                            name,
                                            sheetUnloading.Cells[row, 3]?.Value?.ToString() ?? "",
                                            ownerId);

                                        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                        {
                                            cmd.CommandText = "SELECT LAST_INSERT_ID()";
                                            await _context.Database.OpenConnectionAsync();
                                            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                            unloadingPointIdMapping[oldPointId] = newId;
                                        }
                                    }
                                }

                                // ========== 11. ДОКУМЕНТЫ ==========
                                var sheetDocs = package.Workbook.Worksheets["Документы"];
                                int countDocs = 0;
                                Dictionary<int, int> docIdMapping = new Dictionary<int, int>();

                                if (sheetDocs != null && sheetDocs.Dimension != null && sheetDocs.Dimension.Rows > 1)
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
                                        if (orgIdMapping.ContainsKey(senderId)) senderId = orgIdMapping[senderId];

                                        int carrierId = GetIntValue(sheetDocs.Cells[row, 6]?.Value);
                                        if (orgIdMapping.ContainsKey(carrierId)) carrierId = orgIdMapping[carrierId];

                                        int receiverId = GetIntValue(sheetDocs.Cells[row, 7]?.Value);
                                        if (orgIdMapping.ContainsKey(receiverId)) receiverId = orgIdMapping[receiverId];

                                        int loadingPointId = GetIntValue(sheetDocs.Cells[row, 8]?.Value);
                                        if (loadingPointIdMapping.ContainsKey(loadingPointId)) loadingPointId = loadingPointIdMapping[loadingPointId];

                                        int unloadingPointId = GetIntValue(sheetDocs.Cells[row, 9]?.Value);
                                        if (unloadingPointIdMapping.ContainsKey(unloadingPointId)) unloadingPointId = unloadingPointIdMapping[unloadingPointId];

                                        int driverId = GetIntValue(sheetDocs.Cells[row, 10]?.Value);
                                        if (driverIdMapping.ContainsKey(driverId)) driverId = driverIdMapping[driverId];

                                        int transportId = GetIntValue(sheetDocs.Cells[row, 11]?.Value);
                                        if (transportIdMapping.ContainsKey(transportId)) transportId = transportIdMapping[transportId];

                                        string отпускРазрешил = sheetDocs.Cells[row, 12]?.Value?.ToString() ?? "";
                                        string сдалГрузоотправитель = sheetDocs.Cells[row, 13]?.Value?.ToString() ?? "";

                                        await _context.Database.ExecuteSqlRawAsync(@"
                                    INSERT INTO Документы (номер_документа, дата_создания, 
                                        ид_типа, ид_грузоотправителя, ид_перевозчика, ид_получателя,
                                        ид_пункта_погрузки, ид_пункта_разгрузки, ид_водителя, ид_транспорта,
                                        отпуск_разрешил, сдал_грузоотправитель, ид_пользователя) 
                                    VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, 1)",
                                            docNumber, docDate, typeId,
                                            senderId, carrierId, receiverId,
                                            loadingPointId, unloadingPointId, driverId, transportId,
                                            отпускРазрешил, сдалГрузоотправитель);

                                        var newId = await _context.Документы
                                            .Where(d => d.номер_документа == docNumber && d.дата_создания == docDate)
                                            .OrderByDescending(d => d.ид_документа)
                                            .Select(d => d.ид_документа)
                                            .FirstOrDefaultAsync();

                                        docIdMapping[originalDocId] = newId;
                                        countDocs++;
                                    }
                                }

                                // ========== 12. ПОЗИЦИИ ==========
                                var sheetPositions = package.Workbook.Worksheets["Позиции"];
                                int countPositions = 0;

                                if (sheetPositions != null && sheetPositions.Dimension != null && sheetPositions.Dimension.Rows > 1)
                                {
                                    for (int row = 2; row <= sheetPositions.Dimension.Rows; row++)
                                    {
                                        var docIdObj = sheetPositions.Cells[row, 2]?.Value;
                                        if (docIdObj == null) continue;

                                        int oldDocId = Convert.ToInt32(docIdObj);
                                        if (oldDocId == 0) continue;
                                        if (!docIdMapping.ContainsKey(oldDocId)) continue;

                                        int docId = docIdMapping[oldDocId];
                                        int goodsId = GetIntValue(sheetPositions.Cells[row, 3]?.Value);
                                        if (goodsId == 0) goodsId = 1;
                                        if (goodsIdMapping.ContainsKey(goodsId)) goodsId = goodsIdMapping[goodsId];

                                        double quantity = 0;
                                        if (sheetPositions.Cells[row, 4]?.Value != null)
                                            double.TryParse(sheetPositions.Cells[row, 4].Value.ToString(), out quantity);

                                        decimal price = 0;
                                        if (sheetPositions.Cells[row, 5]?.Value != null)
                                            decimal.TryParse(sheetPositions.Cells[row, 5].Value.ToString(), out price);

                                        object vatRateParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 6]?.Value != null && decimal.TryParse(sheetPositions.Cells[row, 6].Value.ToString(), out decimal vat))
                                            vatRateParam = vat;

                                        object discountParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 7]?.Value != null && decimal.TryParse(sheetPositions.Cells[row, 7].Value.ToString(), out decimal disc))
                                            discountParam = disc;

                                        object weightParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 8]?.Value != null && decimal.TryParse(sheetPositions.Cells[row, 8].Value.ToString(), out decimal weight))
                                            weightParam = weight;

                                        object packagesParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 9]?.Value != null && int.TryParse(sheetPositions.Cells[row, 9].Value.ToString(), out int packages))
                                            packagesParam = packages;

                                        string примечание = sheetPositions.Cells[row, 10]?.Value?.ToString() ?? "";

                                        object vatSumParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 11]?.Value != null && decimal.TryParse(sheetPositions.Cells[row, 11].Value.ToString(), out decimal vatSum))
                                            vatSumParam = vatSum;

                                        object totalWithVatParam = DBNull.Value;
                                        if (sheetPositions.Cells[row, 12]?.Value != null && decimal.TryParse(sheetPositions.Cells[row, 12].Value.ToString(), out decimal totalWithVat))
                                            totalWithVatParam = totalWithVat;

                                        var parameters = new List<MySqlParameter>
                                {
                                    new MySqlParameter("@docId", docId),
                                    new MySqlParameter("@goodsId", goodsId),
                                    new MySqlParameter("@quantity", quantity),
                                    new MySqlParameter("@price", price),
                                    new MySqlParameter("@vatRate", vatRateParam),
                                    new MySqlParameter("@discount", discountParam),
                                    new MySqlParameter("@weight", weightParam),
                                    new MySqlParameter("@packages", packagesParam),
                                    new MySqlParameter("@note", примечание),
                                    new MySqlParameter("@vatSum", vatSumParam),
                                    new MySqlParameter("@totalWithVat", totalWithVatParam)
                                };

                                        await _context.Database.ExecuteSqlRawAsync(@"
                                    INSERT INTO Позиции (ид_документа, ид_товара, количество, цена_за_единицу,
                                        ставка_ндс, скидка, масса_груза, грузовых_мест, примечание, 
                                        сумма_ндс, стоимость_с_ндс) 
                                    VALUES (@docId, @goodsId, @quantity, @price, @vatRate, @discount, 
                                            @weight, @packages, @note, @vatSum, @totalWithVat)",
                                            parameters.ToArray());

                                        countPositions++;
                                    }
                                }

                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
                                await _context.Database.ExecuteSqlRawAsync("SET SQL_SAFE_UPDATES = 1;");

                                await transaction.CommitAsync();

                                TempData["Success"] = $"Восстановлено: Документов: {countDocs}, Позиций: {countPositions}";
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
                                await _context.Database.ExecuteSqlRawAsync("SET SQL_SAFE_UPDATES = 1;");
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
                decimal costWithoutDiscount = pos.цена_за_единицу * quantityDecimal;
                decimal discountAmount = costWithoutDiscount * ((pos.скидка ?? 0) / 100);
                decimal costAfterDiscount = costWithoutDiscount - discountAmount;
                decimal vatAmount = costAfterDiscount * ((pos.ставка_ндс ?? 0) / 100);
                decimal totalWithVat = costAfterDiscount + vatAmount;

                totalQuantity += quantityDecimal;
                totalCost += costAfterDiscount;  // Стоимость после скидки
                totalVat += vatAmount;
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
                        <td class=""right"">{costAfterDiscount:F2}</td>
                        <td class=""right"">{vatAmount:F2}</td>
                        <td class=""right"">{totalWithVat:F2}</td>
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
                .Replace("{{ОтпускРазрешил}}", документ.отпуск_разрешил ?? "")
                .Replace("{{СдалГрузоотправитель}}", документ.сдал_грузоотправитель ?? "")
                .Replace("{{ТоварПринял}}", "")
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
        public async Task<IActionResult> DownloadCmr(int id)
        {
            try
            {
                // 1. Получаем документ
                var документ = await _context.Документы
                    .FirstOrDefaultAsync(d => d.ид_документа == id);

                if (документ == null)
                {
                    return NotFound($"Документ с ID {id} не найден");
                }

                // 2. Получаем позиции
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

                // 3. Получаем связанные данные
                var грузоотправитель = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_грузоотправителя);
                var грузополучатель = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_получателя);
                var перевозчик = await _context.Организации
                    .FirstOrDefaultAsync(o => o.ид_организации == документ.ид_перевозчика);
                var транспорт = await _context.Транспорт
                    .FirstOrDefaultAsync(t => t.ид_транспорта == документ.ид_транспорта);

                if (транспорт != null)
                {
                    await _context.Entry(транспорт)
                        .Reference(t => t.Марка_Транспорта)
                        .LoadAsync();
                }

                var пунктПогрузки = await _context.Пункт_Погрузки
                    .FirstOrDefaultAsync(p => p.ид_пункта_погрузки == документ.ид_пункта_погрузки);
                var пунктРазгрузки = await _context.Пункт_Разгрузки
                    .FirstOrDefaultAsync(p => p.ид_пункта_разгрузки == документ.ид_пункта_разгрузки);

                // 4. Считаем итоги
                decimal totalWeight = позиции.Sum(p => p.weight);
                int totalPackages = позиции.Sum(p => p.packages);
                decimal totalAmount = позиции.Sum(p => {
                    var cost = p.price * (decimal)p.quantity;
                    var discountAmount = cost * (p.discount / 100);
                    return cost - discountAmount;
                });

                // 5. Путь к шаблону Word
                string templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Template",
                    "CMR.docx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return Content($"Файл шаблона не найден по пути: {templatePath}");
                }

                // 6. Создаём временный файл Word
                string tempFile = Path.GetTempFileName();
                string tempFileWithExt = Path.ChangeExtension(tempFile, ".docx");
                System.IO.File.Copy(templatePath, tempFileWithExt, overwrite: true);

                // 7. Заменяем заполнители в Word (РУССКИЕ названия)
                using (WordprocessingDocument doc = WordprocessingDocument.Open(tempFileWithExt, true))
                {
                    var body = doc.MainDocumentPart.Document.Body;

                    var replacements = new Dictionary<string, string>
                    {
                        { "{{sender}}", (грузоотправитель?.название ?? "") + ", " + (грузоотправитель?.адрес ?? "") },
                        { "{{receiver}}", (грузополучатель?.название ?? "") + ", " + (грузополучатель?.адрес ?? "") },
                        { "{{transporter}}", перевозчик?.название ?? "" },
                        { "{{unloading_point}}", пунктРазгрузки?.наименование ?? "" },
                        { "{{loading_point}}", пунктПогрузки?.наименование ?? "" },
                        { "{{date}}", документ.дата_создания.ToString("dd.MM.yyyy") },
                        { "{{reg_number}}", транспорт?.регистрационный_номер ?? "" },
                        { "{{good_name}}", позиции.FirstOrDefault()?.товар_наименование ?? "Не указано" },
                        { "{{weight}}", totalWeight.ToString("F0") + " кг" },
                        { "{{total_sum}}", totalAmount.ToString("N2") },
                        { "{{mark}}", транспорт?.Марка_Транспорта?.наименование_марки ?? "Не указана" }
                    };

                    foreach (var replacement in replacements)
                    {
                        ReplaceTextInBody(body, replacement.Key, replacement.Value);
                    }

                    doc.MainDocumentPart.Document.Save();
                }

                // 8. Конвертируем Word в PDF
                string pdfPath = Path.ChangeExtension(tempFileWithExt, ".pdf");

                var wordDocument = new Spire.Doc.Document();
                wordDocument.LoadFromFile(tempFileWithExt);
                wordDocument.SaveToFile(pdfPath, Spire.Doc.FileFormat.PDF);
                wordDocument.Close();

                // 9. Читаем PDF
                byte[] resultBytes = System.IO.File.ReadAllBytes(pdfPath);

                // 10. Удаляем временные файлы
                System.IO.File.Delete(tempFileWithExt);
                System.IO.File.Delete(pdfPath);

                // 11. Отдаём PDF для просмотра в браузере (без русских символов в заголовке!)
                Response.Headers.Add("Content-Disposition", "inline");
                return File(resultBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return Content($"Ошибка: {ex.Message}<br><br>StackTrace: {ex.StackTrace}");
            }
        }



        // Вспомогательный метод для замены текста в Word-документе
        private void ReplaceTextInBody(Body body, string oldText, string newText)
        {
            // Находим все параграфы
            var paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                // Получаем весь текст параграфа
                string fullText = paragraph.InnerText;

                if (fullText.Contains(oldText))
                {
                    // Заменяем текст
                    var newFullText = fullText.Replace(oldText, newText);

                    // Очищаем параграф
                    paragraph.RemoveAllChildren<DocumentFormat.OpenXml.Wordprocessing.Run>();

                    // Добавляем новый Run с заменённым текстом
                    var newRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
                    newRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(newFullText));
                    paragraph.AppendChild(newRun);
                }
            }
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

                // Определяем тип документа
                var типДокумента = await _context.Типы_Документов
                    .FirstOrDefaultAsync(t => t.ид_типа == документ.ид_типа);

                string documentType = типДокумента?.краткое_наименование?.ToUpper();

                // Если это CMR - генерируем PDF через DownloadCmr
                if (documentType == "CMR")
                {
                    return RedirectToAction("DownloadCmr", new { id = id });
                }

                // Для остальных типов - показываем HTML
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

                var итоги = new DocumentTotals();
                foreach (var pos in позиции)
                {
                    decimal costWithoutDiscount = pos.price * (decimal)pos.quantity;
                    decimal discountAmount = costWithoutDiscount * (pos.discount / 100);
                    decimal costAfterDiscount = costWithoutDiscount - discountAmount;
                    decimal vatRate = pos.ставка_ндс ?? 0;
                    decimal vatAmount = costAfterDiscount * (vatRate / 100);

                    итоги.ВсегоКоличество += (decimal)pos.quantity;
                    итоги.ВсегоСтоимость += costAfterDiscount;
                    итоги.ВсегоСуммаНДС += vatAmount;
                    итоги.ВсегоСтоимостьСНДС += costAfterDiscount + vatAmount;
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

                // Проверяем существование файла вручную
                string templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Views", "Shared", "DocumentTemplates",
                    $"{templateName}.cshtml");

                if (!System.IO.File.Exists(templatePath))
                {
                    templateName = "TTN1";
                }

                return View($"/Views/Shared/DocumentTemplates/{templateName}.cshtml", model);
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