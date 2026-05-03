using Blank.Data;
using Blank.Models.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

            ViewBag.Organizations = _context.Организации.Where(o => o.ид_организации == userOrgId).ToList();

            // ✅ ФИЛЬТРАЦИЯ по организации
            ViewBag.Drivers = _context.Водители.Where(d => d.ид_организации == userOrgId).ToList();
            ViewBag.TransportList = _context.Транспорт.Where(t => t.ид_организации == userOrgId).Include(t => t.Марка_Транспорта).ToList();
            ViewBag.Goods = _context.Товары.Where(g => g.ид_организации == userOrgId).ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.Where(p => p.ид_организации == userOrgId).ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.Where(p => p.ид_организации == userOrgId).ToList();

            ViewBag.TransportMarks = _context.Марка_Транспорта.ToList();
            ViewBag.TransportTypes = _context.Тип_Транспорта.ToList();
            ViewBag.Users = _context.Пользователи.Where(u => u.ид_организации == userOrgId).ToList();

            return View();
        }

        // ==================== ПРОФИЛЬ КОМПАНИИ ====================
        [HttpPost]
        public async Task<IActionResult> UpdateCompany([FromBody] CompanyModel model)
        {
            var org = await _context.Организации.FindAsync(GetUserOrgId());
            if (org == null) return Json(new { success = false });
            org.название = model.Name;
            org.унп = model.Unp;
            org.адрес = model.Address;
            org.почта = model.Email;
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
                ид_организации = GetUserOrgId()  // ✅
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
            _context.Транспорт.Add(new Transport
            {
                регистрационный_номер = model.RegNumber,
                ид_марки = model.BrandId,
                ид_типа_транспорта = model.TypeId,
                ид_организации = GetUserOrgId()  // ✅
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTransport([FromBody] TransportModel model)
        {
            var t = await _context.Транспорт.FindAsync(model.Id);
            if (t == null) return Json(new { success = false });
            t.регистрационный_номер = model.RegNumber;
            t.ид_марки = model.BrandId;
            t.ид_типа_транспорта = model.TypeId;
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
                ид_организации = GetUserOrgId()  // ✅
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
                ид_организации = GetUserOrgId()  // ✅
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
                ид_организации = GetUserOrgId()  // ✅
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

    public class TransportModel
    {
        public int Id { get; set; }
        public string? RegNumber { get; set; }
        public int BrandId { get; set; }
        public int TypeId { get; set; }
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
}