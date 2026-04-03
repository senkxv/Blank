using Microsoft.AspNetCore.Mvc;

namespace Blank.Controllers
{
    public class AuthorizationController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
