using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Single entry-point for the admin area.
    /// Reads Session["UserTypeId"] and redirects to the correct role dashboard.
    /// All four roles land here first; the controller reads TypeId and bounces them.
    ///
    /// TypeId mapping:
    ///   1 → Admin
    ///   2 → Customer
    ///   3 → Vendor
    ///   4 → Manager
    ///   5 → Customer (same role, different TypeId)
    /// </summary>
    [TourwebsiteFYP.Filter.SessionAuthFilter]
    public class DashboardRouterController : Controller
    {
        public ActionResult Index()
        {
            int typeId = 0;
            if (Session["UserTypeId"] != null)
                int.TryParse(Session["UserTypeId"].ToString(), out typeId);

            switch (typeId)
            {
                case 1:
                    return RedirectToAction("Index", "AdminDashboard", new { area = "AdminArea" });
                case 3:
                    return RedirectToAction("Index", "VendorDashboard", new { area = "AdminArea" });
                case 4:
                    return RedirectToAction("Index", "ManagerDashboard", new { area = "AdminArea" });
                case 2:
                case 5:
                default:
                    return RedirectToAction("Index", "CustomerDashboard", new { area = "AdminArea" });
            }
        }
    }
}
