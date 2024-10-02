using Microsoft.AspNetCore.Mvc;

namespace CRDWebAdmin.Data
{
    public class LogCollector : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
