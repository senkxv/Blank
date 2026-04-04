using Microsoft.AspNetCore.Mvc;

namespace Blank.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Authorization()
        {
            return View();
        }
    }
}
