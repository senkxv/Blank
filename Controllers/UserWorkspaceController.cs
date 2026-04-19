using Blank.Data;
using Microsoft.AspNetCore.Mvc;

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
            var главная = _context.Главная.ToList();
            return View(главная);
        }
    }
}
