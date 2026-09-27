using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Customer dashboard. Accessible by TypeId 2 and TypeId 5
    /// (both are named "Customer" in the UserType table).
    /// </summary>
    [SessionAuthFilter]
    [RoleAuthFilter(2, 5)]
    public class CustomerDashboardController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/CustomerDashboard
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "CustomerDashboard";
            ViewBag.Title      = "My Dashboard";

            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            ViewBag.SessionUserName = user?.FullName ?? Session["UserName"]?.ToString() ?? "Customer";
            ViewBag.ProfileUser     = user;

            // Bookings that belong to this customer.
            var myBookings = _context.Bookings
                .Include(b => b.Package)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            ViewBag.TotalBookings   = myBookings.Count;
            ViewBag.ActiveBookings  = myBookings.Count(b => b.Status == "Approved" || b.Status == "Confirmed");
            ViewBag.PendingBookings = myBookings.Count(b => b.Status == "Pending");
            ViewBag.TotalSpent      = myBookings
                .Where(b => b.Status == "Approved" || b.Status == "Confirmed")
                .Sum(b => (decimal?)b.TotalAmount) ?? 0m;

            return View(myBookings);
        }

        // GET: Profile settings partial (shared across roles).
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
