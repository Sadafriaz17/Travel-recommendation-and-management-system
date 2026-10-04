using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;
using TourwebsiteFYP.Helpers;

namespace TourwebsiteFYP.Controllers
{
    public class BookingController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // ─── PACKAGE CHECKOUT ENTRY POINT ───────────────────────────────────────
        // Linked from Package/Index and Package/Details "Book Now" buttons.
        // Instead of rendering a duplicate checkout form, this action adds the
        // package to the user's cart (if not already there) and then forwards to
        // the shared Cart/Checkout view so both products and packages go through
        // exactly the same checkout and thank-you flow.
        [SessionAuthFilter]
        public ActionResult Checkout(int packageId)
        {
            int userId = SessionHelper.GetUserId();

            if (userId == 0)
                return RedirectToAction("Index", "Login");

            var package = _context.Packages
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null)
                return HttpNotFound();

            // Add to cart if not already present, otherwise just refresh the price
            var existing = _context.Carts
                .FirstOrDefault(c =>
                    c.UserId == userId &&
                    c.PackageId == packageId);

            if (existing != null)
            {
                existing.Price = package.Price;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    UserId    = userId,
                    PackageId = packageId,
                    ProductId = null,
                    Quantity  = 1,
                    Price     = package.Price,
                    AddedDate = DateTime.Now
                });
            }

            _context.SaveChanges();

            // Hand off to the shared Cart checkout
            return RedirectToAction("Checkout", "Cart");
        }

        // SHOW BOOKING FORM
        [SessionAuthFilter]
        public ActionResult Create(int packageId)
        {
            var package = _context.Packages
                .Include("Destination")
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null)
                return HttpNotFound();

            var booking = new Booking
            {
                PackageId = packageId,
                BookingDate = DateTime.Now
            };

            ViewBag.Package = package;

            return View(booking);
        }

        // SAVE BOOKING
        [HttpPost]
        [SessionAuthFilter]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Booking booking)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                var package = _context.Packages.Find(booking.PackageId);
                ViewBag.Package = package;
                return View(booking);
            }

            booking.UserId = Convert.ToInt32(Session["UserId"]);
            booking.CreatedDate = DateTime.Now;
            booking.Status = "Pending";

            if (booking.BookingDate == default(DateTime))
                booking.BookingDate = DateTime.Now;

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            TempData["Success"] = "Booking created successfully!";
            return RedirectToAction("Index");
        }

        // USER BOOKING LIST
        [SessionAuthFilter]
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Index", "Login");

            int userId = Convert.ToInt32(Session["UserId"]);

            var bookings = _context.Bookings
                .Include("Package")
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedDate)
                .ToList();

            // For the booking form inside the modal
            ViewBag.Packages = _context.Packages.ToList();
            ViewBag.Destinations = new SelectList(_context.Destinations.ToList(), "DestinationId", "Name");

            return View(bookings);
        }
    }
}