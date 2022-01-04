using System.Web.Mvc;

namespace Fujitsu.eDoc.SAPA.WebService.Controllers
{
    public class HomeController : Controller
    {        
        public ActionResult Index()
        {
            ViewBag.Title = "Forside af SAPA webservice";
            return View();
        }
    }
}
