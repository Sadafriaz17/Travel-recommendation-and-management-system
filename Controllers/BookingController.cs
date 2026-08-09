using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Controllers
{
    public class BookingController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

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