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
        // GET: /UserWorkspace/ExportAllToExcel
        public IActionResult ExportAllToExcel()
        {
            using (var package = new ExcelPackage())
            {
                var sheetDocuments = package.Workbook.Worksheets.Add("Документы");

                sheetDocuments.Cells[1, 1].Value = "Номер документа";
                sheetDocuments.Cells[1, 2].Value = "Тип документа";
                sheetDocuments.Cells[1, 3].Value = "Дата создания";
                sheetDocuments.Cells[1, 4].Value = "Грузоотправитель";
                sheetDocuments.Cells[1, 5].Value = "Перевозчик";
                sheetDocuments.Cells[1, 6].Value = "Грузополучатель";
                sheetDocuments.Cells[1, 7].Value = "Водитель";
                sheetDocuments.Cells[1, 8].Value = "Транспорт (гос. номер)";

                var документы = _context.Документы.ToList();
                int row = 2;

                foreach (var doc in документы)
                {
                    var грузоотправитель = _context.Организации.Find(doc.ид_грузоотправителя);
                    var перевозчик = _context.Организации.Find(doc.ид_перевозчика);
                    var грузополучатель = _context.Организации.Find(doc.ид_получателя);
                    var типДокумента = _context.Типы_Документов.Find(doc.ид_типа);
                    var водитель = _context.Водители.Find(doc.ид_водителя);
                    var транспорт = _context.Транспорт.Find(doc.ид_транспорта);

                    sheetDocuments.Cells[row, 1].Value = doc.номер_документа;
                    sheetDocuments.Cells[row, 2].Value = типДокумента?.краткое_наименование;
                    sheetDocuments.Cells[row, 3].Value = doc.дата_создания.ToString("yyyy-MM-dd");
                    sheetDocuments.Cells[row, 4].Value = грузоотправитель?.название;
                    sheetDocuments.Cells[row, 5].Value = перевозчик?.название;
                    sheetDocuments.Cells[row, 6].Value = грузополучатель?.название;
                    sheetDocuments.Cells[row, 7].Value = водитель != null ? $"{водитель.фамилия} {водитель.имя} {водитель.отчество}" : "";
                    sheetDocuments.Cells[row, 8].Value = транспорт?.регистрационный_номер;
                    row++;
                }
                sheetDocuments.Cells.AutoFitColumns();

                // Остальные листы (Водители, Организации, Транспорт, Товары, Типы документов, Пункты погрузки/разгрузки)
                // ... (оставь как было, но убери все Include)

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Полный_экспорт_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }
    }
    
}