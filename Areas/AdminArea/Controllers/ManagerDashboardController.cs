using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Manager dashboard. Accessible by TypeId 4 only.
    /// Read-only reports and analytics; no raw database or user deletion tools.
    /// </summary>
    [SessionAuthFilter]
    [RoleAuthFilter(4)]
    public class ManagerDashboardController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManagerDashboard
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "ManagerDashboard";
            ViewBag.Title      = "Manager Dashboard";

            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            ViewBag.SessionUserName = user?.FullName ?? Session["UserName"]?.ToString() ?? "Manager";
            ViewBag.ProfileUser     = user;

            // Platform-wide analytics (read-only).
            ViewBag.TotalBookings  = _context.Bookings.Count();
            ViewBag.ConfirmedCount = _context.Bookings.Count(b => b.Status == "Approved" || b.Status == "Confirmed");
            ViewBag.PendingCount   = _context.Bookings.Count(b => b.Status == "Pending");
            ViewBag.CancelledCount = _context.Bookings.Count(b => b.Status == "Rejected" || b.Status == "Cancelled");
            ViewBag.TotalRevenue   = _context.Payments.Any()
                                         ? _context.Payments.Sum(p => p.Amount)
                                         : 0m;
            ViewBag.TotalVendors   = _context.Vendors.Count();
            ViewBag.TotalUsers     = _context.Users.Count();
            ViewBag.TotalPackages  = _context.Packages.Count();
            ViewBag.TotalProducts  = _context.Products.Count();
            ViewBag.TotalReviews   = _context.Reviews.Count();

            ViewBag.AverageRating  = _context.Reviews.Any()
                                         ? Math.Round(_context.Reviews.Average(r => (double?)r.Rating) ?? 0, 1)
                                         : 0.0;

            ViewBag.TopVendors = _context.Vendors
                .Include(v => v.Products)
                .OrderByDescending(v => v.Products.Count)
                .Take(5)
                .ToList();

            var recentBookings = _context.Bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .ToList();

            return View(recentBookings);
        }

        // GET: Profile settings partial.
        public ActionResult ProfileSettings()
        {
            ViewBag.Title = "Profile Settings";

            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            if (user == null) return HttpNotFound();

            return PartialView("~/Areas/AdminArea/Views/Shared/_ProfileSettings.cshtml", user);
        }

        // POST: Save profile.
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
