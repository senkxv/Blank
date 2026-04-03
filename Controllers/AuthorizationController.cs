using Microsoft.AspNetCore.Mvc;

namespace Blank.Controllers
{
    public class AuthorizationController : Controller
    {
        public IActionResult Authorization()
        {
            return View();
        }
    }
}
