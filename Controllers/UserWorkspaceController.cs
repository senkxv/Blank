using Blank.Data;
using Blank.Models.Tables;
using Blank.Models.Views;
using Microsoft.AspNetCore.Mvc;
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

        // GET: /UserWorkspace/CreateDocumentPage
        public IActionResult CreateDocumentPage()
        {
            // Загружаем данные для выпадающих списков
            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View();
        }

        // POST: /UserWorkspace/CreateDocumentPage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDocumentPage(Documents document)
        {
            if (ModelState.IsValid)
            {
                _context.Документы.Add(document);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Если ошибка — перезагружаем списки
            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View(document);
        }

        // GET: /UserWorkspace/Edit/5
        // GET: /UserWorkspace/EditDocumentPage/5
        public IActionResult EditDocumentPage(int id)
        {
            var документ = _context.Документы.Find(id);
            if (документ == null)
            {
                return NotFound();
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View(документ);
        }

        // POST: /UserWorkspace/EditDocumentPage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDocumentPage(int id, Documents document)
        {
            if (id != document.ид_документа)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(document);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View(document);
        }
    }
}