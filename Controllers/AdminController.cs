using Blank.Data;
using Blank.Models.Tables;
using Blank.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Blank.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AdminController(ApplicationDBContext context)
        {
            _context = context;
        }

        private bool IsCompanyAdmin()
        {
            var roleId = HttpContext.Session.GetString("UserRoleId");
            return roleId == "1";
        }

        private int? GetUserOrgId()
        {
            var orgId = HttpContext.Session.GetString("UserOrgId");
            return string.IsNullOrEmpty(orgId) ? null : int.Parse(orgId);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }

        // ==================== ГЛАВНАЯ СТРАНИЦА ====================
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsCompanyAdmin())
                return RedirectToAction("Index", "UserWorkspace");

            var userOrgId = GetUserOrgId();

            ViewBag.Organizations = _context.Организации
                .Where(o => o.ид_организации == userOrgId || o.ид_владельца == userOrgId)
                .ToList();
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.TransportList = _context.Транспорт.Where(t => t.ид_организации == userOrgId).Include(t => t.Марка_Транспорта).ToList();
            ViewBag.TransportMarks = _context.Марка_Транспорта.ToList();
            ViewBag.TransportTypes = _context.Тип_Транспорта.ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.Users = _context.Пользователи.Where(u => u.ид_организации == userOrgId).ToList();
            ViewBag.Transport = _context.Транспорт.Where(t => t.ид_организации == userOrgId).ToList();

            // Загрузка маршрутов
            ViewBag.Routes = _context.Маршруты
                .Include(r => r.Водитель)
                .Include(r => r.Транспорт)
                .Include(r => r.Перевозчик)
                .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                    .ThenInclude(t => t.ПунктПогрузки)
                .Include(r => r.ТочкиМаршрута)
                    .ThenInclude(t => t.ПунктРазгрузки)
                .Where(r => r.ид_организации == userOrgId)
                .ToList();

            return View();
        }

        // ==================== МАРШРУТЫ ====================
        [HttpPost]
        public async Task<IActionResult> CreateRoute(string routeName, string driverId, string transportId, string carrierId, string routePointsData)
        {
            try
            {
                if (string.IsNullOrEmpty(routeName))
                {
                    TempData["Error"] = "Название маршрута обязательно!";
                    return RedirectToAction("Index");
                }

                var userOrgIdStr = HttpContext.Session.GetString("UserOrgId");
                int userOrgId = string.IsNullOrEmpty(userOrgIdStr) ? 106 : int.Parse(userOrgIdStr);

                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                await _context.Database.ExecuteSqlRawAsync(
    "INSERT INTO маршруты (название, ид_организации, ид_водителя, ид_транспорта, ид_перевозчика, ид_типа, статус) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
    routeName, userOrgId,
    string.IsNullOrEmpty(driverId) ? null : int.Parse(driverId),
    string.IsNullOrEmpty(transportId) ? null : int.Parse(transportId),
    string.IsNullOrEmpty(carrierId) ? null : int.Parse(carrierId),
    1, // ТТН
    "активен");

                // Получаем ID созданного маршрута
                var routeId = await _context.Маршруты
                    .OrderByDescending(r => r.ид_маршрута)
                    .Select(r => r.ид_маршрута)
                    .FirstOrDefaultAsync();

                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

                // Сохраняем точки маршрута
                if (!string.IsNullOrEmpty(routePointsData) && routeId > 0)
                {
                    var points = JsonSerializer.Deserialize<List<RoutePointViewModel>>(routePointsData);
                    if (points != null)
                    {
                        int order = 1;
                        foreach (var point in points)
                        {
                            await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO точки_маршрута (ид_маршрута, порядковый_номер, ид_грузоотправителя, ид_пункта_погрузки, ид_пункта_разгрузки, ид_грузополучателя, тип_точки) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                                routeId, order++,
                                point.ид_грузоотправителя,
                                point.ид_пункта_погрузки,
                                point.ид_пункта_разгрузки,
                                point.ид_грузополучателя,
                                point.тип_точки ?? "погрузка");
                            await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
                        }
                    }
                }

                TempData["Success"] = "Маршрут '" + routeName + "' создан с точками!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ошибка: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Получить маршрут для редактирования
        [HttpGet]
        public async Task<IActionResult> GetRoute(int id)
        {
            var route = await _context.Маршруты
                .Include(r => r.ТочкиМаршрута.OrderBy(t => t.порядковый_номер))
                .FirstOrDefaultAsync(r => r.ид_маршрута == id);

            if (route == null) return NotFound();

            return Json(new
            {
                route.ид_маршрута,
                route.название,
                route.ид_водителя,
                route.ид_транспорта,
                route.ид_перевозчика,
                route.статус,
                точки = route.ТочкиМаршрута.Select(t => new {
                    t.ид_точки,
                    t.порядковый_номер,
                    t.ид_грузоотправителя,
                    t.ид_пункта_погрузки,
                    t.ид_пункта_разгрузки,
                    t.ид_грузополучателя
                })
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRoute([FromBody] UpdateRouteRequest request)
        {
            try
            {
                var route = await _context.Маршруты
                    .Include(r => r.ТочкиМаршрута)
                    .FirstOrDefaultAsync(r => r.ид_маршрута == request.id);

                if (route == null)
                    return NotFound(new { error = "Маршрут не найден" });

                route.название = request.routeName;
                route.ид_водителя = string.IsNullOrEmpty(request.driverId) ? null : int.Parse(request.driverId);
                route.ид_транспорта = string.IsNullOrEmpty(request.transportId) ? null : int.Parse(request.transportId);
                route.ид_перевозчика = string.IsNullOrEmpty(request.carrierId) ? null : int.Parse(request.carrierId);
                route.статус = request.status ?? "активен";

                // Обновляем точки
                if (!string.IsNullOrEmpty(request.routePointsData))
                {
                    var points = JsonSerializer.Deserialize<List<RoutePointUpdateModel>>(request.routePointsData);

                    // Удаляем старые точки
                    var oldPoints = _context.Точки_Маршрута.Where(t => t.ид_маршрута == request.id);
                    _context.Точки_Маршрута.RemoveRange(oldPoints);
                    await _context.SaveChangesAsync();

                    // Добавляем новые
                    if (points != null)
                    {
                        foreach (var point in points)
                        {
                            var routePoint = new RoutePoint
                            {
                                ид_маршрута = request.id,
                                порядковый_номер = point.порядковый_номер,
                                ид_грузоотправителя = point.ид_грузоотправителя,
                                ид_пункта_погрузки = point.ид_пункта_погрузки,
                                ид_пункта_разгрузки = point.ид_пункта_разгрузки,
                                ид_грузополучателя = point.ид_грузополучателя,
                                тип_точки = "погрузка"
                            };
                            _context.Точки_Маршрута.Add(routePoint);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            try
            {
                var route = await _context.Маршруты.FindAsync(id);
                if (route == null)
                {
                    return NotFound(new { error = "Маршрут не найден" });
                }

                var points = _context.Точки_Маршрута.Where(p => p.ид_маршрута == id);
                _context.Точки_Маршрута.RemoveRange(points);
                _context.Маршруты.Remove(route);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== ОРГАНИЗАЦИИ ====================
        [HttpPost]
        public async Task<IActionResult> AddOrganization([FromBody] CompanyModel model)
        {
            _context.Организации.Add(new Organization
            {
                название = model.Name,
                унп = model.Unp,
                адрес = model.Address,
                почта = model.Email,
                ид_владельца = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateOrganization([FromBody] CompanyModel model)
        {
            var org = await _context.Организации.FindAsync(model.Id);
            if (org == null) return Json(new { success = false });
            org.название = model.Name;
            org.унп = model.Unp;
            org.адрес = model.Address;
            org.почта = model.Email;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var org = await _context.Организации.FindAsync(id);
            if (org == null) return Json(new { success = false });
            _context.Организации.Remove(org);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ВОДИТЕЛИ ====================
        [HttpPost]
        public async Task<IActionResult> AddDriver([FromBody] DriverModel model)
        {
            _context.Водители.Add(new Drivers
            {
                фамилия = model.LastName,
                имя = model.FirstName,
                отчество = model.MiddleName,
                номер_лицензии = model.LicenseNumber,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateDriver([FromBody] DriverModel model)
        {
            var d = await _context.Водители.FindAsync(model.Id);
            if (d == null) return Json(new { success = false });
            d.фамилия = model.LastName;
            d.имя = model.FirstName;
            d.отчество = model.MiddleName;
            d.номер_лицензии = model.LicenseNumber;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var d = await _context.Водители.FindAsync(id);
            if (d == null) return Json(new { success = false });
            _context.Водители.Remove(d);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ТРАНСПОРТ ====================
        [HttpPost]
        public async Task<IActionResult> AddTransport([FromBody] TransportModel model)
        {
            var brand = await _context.Марка_Транспорта
                .FirstOrDefaultAsync(m => m.наименование_марки == model.BrandName);
            if (brand == null)
            {
                brand = new Transport_Mark { наименование_марки = model.BrandName };
                _context.Марка_Транспорта.Add(brand);
                await _context.SaveChangesAsync();
            }

            var type = await _context.Тип_Транспорта
                .FirstOrDefaultAsync(t => t.наименование_типа == model.TypeName);
            if (type == null)
            {
                type = new Transport_Type { наименование_типа = model.TypeName };
                _context.Тип_Транспорта.Add(type);
                await _context.SaveChangesAsync();
            }

            _context.Транспорт.Add(new Transport
            {
                регистрационный_номер = model.RegNumber,
                ид_марки = brand.ид_марки,
                ид_типа_транспорта = type.ид_типа_транспорта,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTransport([FromBody] TransportModel model)
        {
            var t = await _context.Транспорт.FindAsync(model.Id);
            if (t == null) return Json(new { success = false });

            var brand = await _context.Марка_Транспорта
                .FirstOrDefaultAsync(m => m.наименование_марки == model.BrandName);
            if (brand == null)
            {
                brand = new Transport_Mark { наименование_марки = model.BrandName };
                _context.Марка_Транспорта.Add(brand);
                await _context.SaveChangesAsync();
            }

            var type = await _context.Тип_Транспорта
                .FirstOrDefaultAsync(t => t.наименование_типа == model.TypeName);
            if (type == null)
            {
                type = new Transport_Type { наименование_типа = model.TypeName };
                _context.Тип_Транспорта.Add(type);
                await _context.SaveChangesAsync();
            }

            t.регистрационный_номер = model.RegNumber;
            t.ид_марки = brand.ид_марки;
            t.ид_типа_транспорта = type.ид_типа_транспорта;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTransport(int id)
        {
            var t = await _context.Транспорт.FindAsync(id);
            if (t == null) return Json(new { success = false });
            _context.Транспорт.Remove(t);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ТОВАРЫ ====================
        [HttpPost]
        public async Task<IActionResult> AddGoods([FromBody] GoodsModel model)
        {
            _context.Товары.Add(new Goods
            {
                код_товара = model.Code,
                наименование = model.Name,
                единицы_измерения = model.Unit,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateGoods([FromBody] GoodsModel model)
        {
            var g = await _context.Товары.FindAsync(model.Id);
            if (g == null) return Json(new { success = false });
            g.код_товара = model.Code;
            g.наименование = model.Name;
            g.единицы_измерения = model.Unit;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGoods(int id)
        {
            var g = await _context.Товары.FindAsync(id);
            if (g == null) return Json(new { success = false });
            _context.Товары.Remove(g);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ПУНКТЫ ПОГРУЗКИ ====================
        [HttpPost]
        public async Task<IActionResult> AddLoadingPoint([FromBody] PointModel model)
        {
            _context.Пункт_Погрузки.Add(new Loading_Point
            {
                наименование = model.Name,
                адрес = model.Address,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateLoadingPoint([FromBody] PointModel model)
        {
            var p = await _context.Пункт_Погрузки.FindAsync(model.Id);
            if (p == null) return Json(new { success = false });
            p.наименование = model.Name;
            p.адрес = model.Address;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLoadingPoint(int id)
        {
            var p = await _context.Пункт_Погрузки.FindAsync(id);
            if (p == null) return Json(new { success = false });
            _context.Пункт_Погрузки.Remove(p);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ПУНКТЫ РАЗГРУЗКИ ====================
        [HttpPost]
        public async Task<IActionResult> AddUnloadingPoint([FromBody] PointModel model)
        {
            _context.Пункт_Разгрузки.Add(new Unloading_Point
            {
                наименование = model.Name,
                адрес = model.Address,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUnloadingPoint([FromBody] PointModel model)
        {
            var p = await _context.Пункт_Разгрузки.FindAsync(model.Id);
            if (p == null) return Json(new { success = false });
            p.наименование = model.Name;
            p.адрес = model.Address;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUnloadingPoint(int id)
        {
            var p = await _context.Пункт_Разгрузки.FindAsync(id);
            if (p == null) return Json(new { success = false });
            _context.Пункт_Разгрузки.Remove(p);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== ПОЛЬЗОВАТЕЛИ ====================
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserModel model)
        {
            _context.Пользователи.Add(new Users
            {
                почта = model.Email,
                хэш_пароль = HashPassword(model.Password),
                фамилия = model.LastName,
                имя = model.FirstName,
                отчество = model.MiddleName,
                активность = true,
                ид_должности = model.RoleId,
                ид_организации = GetUserOrgId()
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserRole([FromBody] UserModel model)
        {
            var u = await _context.Пользователи.FindAsync(model.Id);
            if (u == null) return Json(new { success = false });
            u.ид_должности = model.RoleId;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var u = await _context.Пользователи.FindAsync(id);
            if (u == null) return Json(new { success = false });
            _context.Пользователи.Remove(u);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // ==================== МОДЕЛИ ДЛЯ AJAX ====================
    public class CompanyModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Unp { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
    }

    public class DriverModel
    {
        public int Id { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LicenseNumber { get; set; }
    }

    public class RoutePointUpdateModel
    {
        public int? ид_точки { get; set; }
        public int? ид_грузоотправителя { get; set; }
        public int? ид_пункта_погрузки { get; set; }
        public int? ид_пункта_разгрузки { get; set; }
        public int? ид_грузополучателя { get; set; }
        public int порядковый_номер { get; set; }
    }

    public class TransportModel
    {
        public int Id { get; set; }
        public string? RegNumber { get; set; }
        public string? BrandName { get; set; }
        public string? TypeName { get; set; }
    }

    public class GoodsModel
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
    }

    public class PointModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? Password { get; set; }
        public int RoleId { get; set; }
    }

    public class CreateRouteRequest
    {
        public string routeName { get; set; }
        public string driverId { get; set; }
        public string transportId { get; set; }
        public string carrierId { get; set; }
        public string routePointsData { get; set; }
    }

    public class UpdateRouteRequest
    {
        public int id { get; set; }
        public string routeName { get; set; }
        public string driverId { get; set; }
        public string transportId { get; set; }
        public string carrierId { get; set; }
        public string status { get; set; }
        public string routePointsData { get; set; }
    }
}