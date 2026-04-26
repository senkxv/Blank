using Blank.Data;
using Blank.Models.Tables;
using Blank.Models.Views;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Text;

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

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return View(document);
        }

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
                return RedirectToAction("Index", "UserWorkspace");
            }

            ViewBag.DocumentTypes = _context.Типы_Документов.ToList();
            ViewBag.Organizations = _context.Организации.ToList();
            ViewBag.Drivers = _context.Водители.ToList();
            ViewBag.Transport = _context.Транспорт.ToList();
            ViewBag.LoadingPoints = _context.Пункт_Погрузки.ToList();
            ViewBag.UnloadingPoints = _context.Пункт_Разгрузки.ToList();

            return RedirectToAction("Index", "UserWorkspace");
        }

        // GET: /UserWorkspace/DeleteDocument?id=5
        public IActionResult DeleteDocument(int id)
        {
            var документ = _context.Документы.Find(id);
            if (документ == null)
            {
                return NotFound();
            }

            _context.Документы.Remove(документ);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: /UserWorkspace/ExportAllToExcel
        public IActionResult ExportAllToExcel()
        {
            using (var package = new ExcelPackage())
            {
                // === 1. Лист "Документы" (с расшифровками) ===
                var sheetDocuments = package.Workbook.Worksheets.Add("Документы");

                // Заголовки (все 14 столбцов)
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

                // Автоширина столбцов
                sheetDocuments.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Документы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }

        // GET: /UserWorkspace/Search?searchString=...
        public IActionResult Search(string searchString)
        {
            var данные = _context.Главная.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                данные = данные.Where(d =>
                    d.ид_документа.ToString().Contains(searchString) ||
                    (d.тип != null && d.тип.ToLower().Contains(searchString)) ||
                    (d.номер_документа != null && d.номер_документа.ToLower().Contains(searchString)) ||
                    (d.грузоотправитель != null && d.грузоотправитель.ToLower().Contains(searchString)) ||
                    (d.перевозчик != null && d.перевозчик.ToLower().Contains(searchString)) ||
                    (d.грузополучатель != null && d.грузополучатель.ToLower().Contains(searchString)) ||
                    (d.пункт_погрузки != null && d.пункт_погрузки.ToLower().Contains(searchString)) ||
                    (d.пункт_разгрузки != null && d.пункт_разгрузки.ToLower().Contains(searchString))
                );
            }

            ViewBag.SearchString = searchString;
            return View("Index", данные.ToList());
        }
    }
}