using Microsoft.AspNetCore.Mvc;

namespace Blank.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Authorization()
        {
            return View();
        }

        public IActionResult Registration()
        {
            return View();
        }
    }
}
