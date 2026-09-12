using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class AdminDashboardController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/AdminDashboard
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Dashboard";
            ViewBag.Title = "Dashboard";

            ViewBag.TotalBookings   = _context.Bookings.Count();
            ViewBag.TotalUsers      = _context.Users.Count();
            ViewBag.TotalPackages   = _context.Packages.Count();
            ViewBag.TotalRevenue    = _context.Payments.Any()
                                        ? _context.Payments.Sum(p => p.Amount)
                                        : 0m;
            ViewBag.TotalDestinations = _context.Destinations.Count();
            ViewBag.TotalProducts   = _context.Products.Count();

            var recentBookings = _context.Bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(8)
                .ToList();

            return View(recentBookings);
        }
    }
}