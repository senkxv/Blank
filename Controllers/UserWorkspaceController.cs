using Microsoft.AspNetCore.Mvc;

namespace Blank.Controllers
{
    public class UserWorkspaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
