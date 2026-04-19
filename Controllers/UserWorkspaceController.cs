using Blank.Data;
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

        public IActionResult Index()
        {
            var данные = _context.Главная.ToList();
            return View(данные);
        }
    }
}