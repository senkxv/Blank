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

                    // ✅ ДОБАВИТЬ: сохраняем ID организации и должности
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

                int? orgId = null;
                int? должностьId = 3; // По умолчанию обычный пользователь

                // Если включён режим администратора — создаём новую организацию
                if (model.IsAdmin)
                {
                    var newOrg = new Organization
                    {
                        название = "",  // Пустое, админ заполнит сам
                        унп = "",
                        адрес = "",
                        почта = ""     // Пустое, админ заполнит сам
                    };
                    _context.Организации.Add(newOrg);
                    await _context.SaveChangesAsync();

                    orgId = newOrg.ид_организации;
                    должностьId = 1; 
                }

                var user = new Users
                {
                    почта = model.Email,
                    хэш_пароль = HashPassword(model.Password ?? ""),
                    фамилия = фамилия,
                    имя = имя,
                    отчество = отчество,
                    активность = true,
                    ид_должности = должностьId,
                    ид_организации = orgId
                };

                _context.Пользователи.Add(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserId", user.ид_пользователя.ToString());
                HttpContext.Session.SetString("UserEmail", user.почта ?? "");
                HttpContext.Session.SetString("UserName", $"{user.фамилия} {user.имя}");

                return RedirectToAction("Index", "UserWorkspace");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Authorization");
        }
    }
}