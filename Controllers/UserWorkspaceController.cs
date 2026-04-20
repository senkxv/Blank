using Blank.Data;
using Blank.Models.Tables;
using Blank.Models.Views;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Blank.Controllers
{
    public class UserWorkspaceController : Controller
    {
        private readonly ApplicationDBContext _context;

        public UserWorkspaceController(ApplicationDBContext context)
        {
            _context = context;
        }

        // Главная страница с таблицей документов
        public IActionResult Index()
        {
            var данные = _context.Главная.ToList();
            return View(данные);
        }

        // GET: /UserWorkspace/Create
        public IActionResult CreateDocumentPage()
        {
            // Загружаем данные для выпадающих списков
            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();

            // Убираем Include — грузим только транспорт
            ViewBag.Transport = _context.Транспорт.ToList();

            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View();
        }
    }
}