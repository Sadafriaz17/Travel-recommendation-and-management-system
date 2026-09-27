using System.Web.Mvc;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Legacy stub - redirects to DashboardRouter so existing links are not broken.
    /// </summary>
    [SessionAuthFilter]
    public class UserDashboardController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "DashboardRouter", new { area = "AdminArea" });
        }
    }
}