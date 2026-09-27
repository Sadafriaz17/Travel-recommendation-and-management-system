using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// Vendor dashboard. Accessible by TypeId 3 only.
    /// Shows only data that belongs to this vendor's marketplace listing.
    /// </summary>
    [SessionAuthFilter]
    [RoleAuthFilter(3)]
    public class VendorDashboardController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/VendorDashboard
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "VendorDashboard";
            ViewBag.Title      = "Vendor Dashboard";

            int userId = 0;
            if (Session["UserId"] != null)
                int.TryParse(Session["UserId"].ToString(), out userId);

            var user = _context.Users.Find(userId);
            ViewBag.SessionUserName = user?.FullName ?? Session["UserName"]?.ToString() ?? "Vendor";
            ViewBag.ProfileUser     = user;

            // Look up vendor associated with this user's name.
            // (The current schema has no direct UserId FK on Vendor.)
            string userName = ViewBag.SessionUserName as string ?? "";
            var vendor = _context.Vendors
                             .Include(v => v.Products)
                             .FirstOrDefault(v => v.VendorName.Contains(userName))
                         ?? _context.Vendors.Include(v => v.Products).FirstOrDefault();

            if (vendor != null)
            {
                ViewBag.VendorName    = vendor.VendorName;
                ViewBag.VendorRating  = vendor.Rating;
                ViewBag.TotalProducts = vendor.Products.Count;
                ViewBag.VendorId      = vendor.VendorId;

                var vendorProductIds = vendor.Products.Select(p => p.ProductId).ToList();
                var vendorBookings = _context.BookingDetails
                    .Include(bd => bd.Booking)
                    .Include(bd => bd.Product)
                    .Where(bd => bd.ProductId != null && vendorProductIds.Contains(bd.ProductId.Value))
                    .ToList();

                ViewBag.TotalOrders   = vendorBookings.Select(bd => bd.BookingId).Distinct().Count();
                ViewBag.TotalEarnings = vendorBookings.Sum(bd => (decimal?)bd.Price) ?? 0m;

                var recentOrders = vendorBookings
                    .OrderByDescending(bd => bd.Booking?.BookingDate)
                    .Take(8)
                    .ToList();

                return View(recentOrders);
            }

            ViewBag.VendorName    = userName;
            ViewBag.TotalProducts = 0;
            ViewBag.TotalOrders   = 0;
            ViewBag.TotalEarnings = 0m;
            ViewBag.VendorRating  = 0m;
            return View(new System.Collections.Generic.List<BookingDetail>());
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
