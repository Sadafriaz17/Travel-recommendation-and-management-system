using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Admin-only dashboard. TypeId 1 only.
    /// Any other role that somehow reaches this URL is bounced by RoleAuthFilter
    /// back to DashboardRouter which redirects them to their correct dashboard.
    /// </summary>
    [SessionAuthFilter]
    [RoleAuthFilter(1)]
    public class AdminDashboardController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/AdminDashboard
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Dashboard";
            ViewBag.Title      = "Dashboard";

            // Load logged-in user for session display and profile settings partial.
            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            ViewBag.SessionUserName = user?.FullName ?? Session["UserName"]?.ToString() ?? "Admin";
            ViewBag.ProfileUser     = user;

            // Platform stats
            ViewBag.TotalBookings     = _context.Bookings.Count();
            ViewBag.TotalUsers        = _context.Users.Count();
            ViewBag.TotalPackages     = _context.Packages.Count();
            ViewBag.TotalRevenue      = _context.Payments.Any()
                                            ? _context.Payments.Sum(p => p.Amount)
                                            : 0m;
            ViewBag.TotalDestinations = _context.Destinations.Count();
            ViewBag.TotalProducts     = _context.Products.Count();

            // Pending vendor approvals — pre-compute threshold so EF6 can translate to SQL.
            var sevenDaysAgo       = DateTime.Now.AddDays(-7);
            ViewBag.PendingVendors = _context.Vendors.Count(v => v.CreatedAt >= sevenDaysAgo);

            var recentBookings = _context.Bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(8)
                .ToList();

            return View(recentBookings);
        }

        // POST: Save profile — called by the shared _ProfileSettings partial.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveProfile(string FullName, string Phone, string Address)
        {
            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            if (user == null) return HttpNotFound();

            user.FullName = FullName?.Trim() ?? user.FullName;
            user.Phone    = Phone?.Trim();
            user.Address  = Address?.Trim();
            _context.SaveChanges();

            Session["UserName"] = user.FullName;
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Index");
        }
    }
}