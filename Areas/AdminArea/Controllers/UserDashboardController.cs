using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    public class UserDashboardController : Controller
    {
        // GET: AdminArea/UserDashboard
        public ActionResult Index()
        {
            return View();
        }
    }
}