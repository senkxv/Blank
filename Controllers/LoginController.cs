using Blank.Data;
using Blank.Models.Tables;
using Blank.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Blank.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDBContext _context;

        public LoginController(ApplicationDBContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        [HttpGet]
        public IActionResult Authorization()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authorization(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Пользователи
                    .FirstOrDefaultAsync(u => u.почта == model.Email);

                if (user != null && HashPassword(model.Password ?? "") == user.хэш_пароль)
                {
                    HttpContext.Session.SetString("UserId", user.ид_пользователя.ToString());
                    HttpContext.Session.SetString("UserEmail", user.почта ?? "");
                    HttpContext.Session.SetString("UserName", $"{user.фамилия} {user.имя}");
                    HttpContext.Session.SetString("UserOrgId", user.ид_организации?.ToString() ?? "");
                    HttpContext.Session.SetString("UserRoleId", user.ид_должности?.ToString() ?? "");

                    return RedirectToAction("Index", "UserWorkspace");
                }

                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var fioParts = model.ФИО?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var фамилия = fioParts?.Length > 0 ? fioParts[0] : "";
                var имя = fioParts?.Length > 1 ? fioParts[1] : "";
                var отчество = fioParts?.Length > 2 ? fioParts[2] : "";

                var existingUser = await _context.Пользователи
                    .FirstOrDefaultAsync(u => u.почта == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Пользователь с таким email уже существует");
                    return View(model);
                }

                var newOrg = new Organization
                {
                    название = model.IsAdmin ? "Компания " + Guid.NewGuid().ToString("N").Substring(0, 8) : "Моя организация",
                    унп = "",
                    адрес = "",
                    почта = model.Email ?? "",
                    ид_владельца = null
                };
                _context.Организации.Add(newOrg);
                await _context.SaveChangesAsync();

                int? должностьId = model.IsAdmin ? 1 : 3;

                var user = new Users
                {
                    почта = model.Email ?? "",
                    хэш_пароль = HashPassword(model.Password ?? ""),
                    фамилия = фамилия,
                    имя = имя,
                    отчество = отчество,
                    активность = true,
                    ид_должности = должностьId,
                    ид_организации = newOrg.ид_организации
                };

                _context.Пользователи.Add(user);
                await _context.SaveChangesAsync();

                newOrg.ид_владельца = user.ид_пользователя;
                await _context.SaveChangesAsync();

                if (!model.IsAdmin)
                {
                    await CreateDemoData(user.ид_пользователя, newOrg.ид_организации);
                }

                HttpContext.Session.SetString("UserId", user.ид_пользователя.ToString());
                HttpContext.Session.SetString("UserEmail", user.почта);
                HttpContext.Session.SetString("UserName", $"{user.фамилия} {user.имя}");
                HttpContext.Session.SetString("UserOrgId", user.ид_организации?.ToString() ?? "");
                HttpContext.Session.SetString("UserRoleId", user.ид_должности?.ToString() ?? "");

                return RedirectToAction("Index", "UserWorkspace");
            }
            return View(model);
        }

        private async Task CreateDemoData(int userId, int orgId)
        {
            var drivers = new List<Drivers>
            {
                new Drivers { фамилия = "Иванов", имя = "Иван", отчество = "Иванович", номер_лицензии = "AA123456", ид_организации = orgId },
                new Drivers { фамилия = "Петров", имя = "Пётр", отчество = "Петрович", номер_лицензии = "BB654321", ид_организации = orgId },
                new Drivers { фамилия = "Сидоров", имя = "Сергей", отчество = "Сергеевич", номер_лицензии = "CC789012", ид_организации = orgId }
            };
            _context.Водители.AddRange(drivers);

            var loadingPoints = new List<Loading_Point>
            {
                new Loading_Point { наименование = "Склад №1", адрес = "Минск, ул. Ленина, 1", ид_организации = orgId },
                new Loading_Point { наименование = "Склад №2", адрес = "Минск, ул. Пушкина, 10", ид_организации = orgId },
                new Loading_Point { наименование = "База", адрес = "Минск, ул. Советская, 5", ид_организации = orgId }
            };
            _context.Пункт_Погрузки.AddRange(loadingPoints);

            var unloadingPoints = new List<Unloading_Point>
            {
                new Unloading_Point { наименование = "Магазин №1", адрес = "Гомель, ул. Кирова, 15", ид_организации = orgId },
                new Unloading_Point { наименование = "Магазин №2", адрес = "Гродно, ул. Мира, 20", ид_организации = orgId },
                new Unloading_Point { наименование = "ТЦ Центральный", адрес = "Брест, ул. Московская, 8", ид_организации = orgId }
            };
            _context.Пункт_Разгрузки.AddRange(unloadingPoints);

            var goods = new List<Goods>
            {
                new Goods { код_товара = "Т001", наименование = "Цемент", единицы_измерения = "кг", ид_организации = orgId },
                new Goods { код_товара = "Т002", наименование = "Доска обрезная", единицы_измерения = "м³", ид_организации = orgId },
                new Goods { код_товара = "Т003", наименование = "Кирпич", единицы_измерения = "шт", ид_организации = orgId }
            };
            _context.Товары.AddRange(goods);

            var mark = await _context.Марка_Транспорта.FirstOrDefaultAsync(m => m.наименование_марки == "MAN")
                       ?? new Transport_Mark { наименование_марки = "MAN" };
            if (mark.ид_марки == 0) { _context.Марка_Транспорта.Add(mark); await _context.SaveChangesAsync(); }

            var mark2 = await _context.Марка_Транспорта.FirstOrDefaultAsync(m => m.наименование_марки == "Volvo")
                        ?? new Transport_Mark { наименование_марки = "Volvo" };
            if (mark2.ид_марки == 0) { _context.Марка_Транспорта.Add(mark2); await _context.SaveChangesAsync(); }

            var type = await _context.Тип_Транспорта.FirstOrDefaultAsync(t => t.наименование_типа == "Фура")
                       ?? new Transport_Type { наименование_типа = "Фура" };
            if (type.ид_типа_транспорта == 0) { _context.Тип_Транспорта.Add(type); await _context.SaveChangesAsync(); }

            var type2 = await _context.Тип_Транспорта.FirstOrDefaultAsync(t => t.наименование_типа == "Газель")
                        ?? new Transport_Type { наименование_типа = "Газель" };
            if (type2.ид_типа_транспорта == 0) { _context.Тип_Транспорта.Add(type2); await _context.SaveChangesAsync(); }

            var transports = new List<Transport>
            {
                new Transport { регистрационный_номер = "AA1234-1", ид_марки = mark.ид_марки, ид_типа_транспорта = type.ид_типа_транспорта, ид_организации = orgId },
                new Transport { регистрационный_номер = "BB5678-1", ид_марки = mark2.ид_марки, ид_типа_транспорта = type.ид_типа_транспорта, ид_организации = orgId },
                new Transport { регистрационный_номер = "CC9012-1", ид_марки = mark.ид_марки, ид_типа_транспорта = type2.ид_типа_транспорта, ид_организации = orgId }
            };
            _context.Транспорт.AddRange(transports);

            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Authorization");
        }
    }
}