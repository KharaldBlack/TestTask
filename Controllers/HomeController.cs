using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TestTask.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Departments()
        {
            return RedirectToAction("Index", "Departments");
        }

        public IActionResult Employees()
        {
            return RedirectToAction("Index", "Employees");
        }
    }
}
