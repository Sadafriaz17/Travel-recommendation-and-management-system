using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using System.Data.Entity;

namespace TourwebsiteFYP.Controllers
{
    public class PackageController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // ── 1. MAIN PACKAGES PAGE ──
        public ActionResult Index()
        {
            var packages = _context.Packages
                .Include(p => p.Destination)
                .Include(p => p.PackageType)
                .Include(p => p.PackageImages)
                .ToList();
            return View(packages);
        }

        // ── 2. HOMEPAGE FEATURED PACKAGES ──
        public ActionResult Homepagepackage()
        {
            var packages = _context.Packages
                .Include(p => p.Destination)
                .Include(p => p.PackageImages)
                .OrderBy(d => Guid.NewGuid())
                .Take(3)
                .ToList();
            return View(packages);
        }

        // ── 3. PACKAGE DETAILS PAGE ──
        public ActionResult Details(int id)
        {
            try
            {
                var package = _context.Packages
                    .Include(p => p.Destination)
                    .Include(p => p.PackageType)
                    .Include(p => p.PackageImages)
                    .Include(p => p.PackageItineraries)
                    .Include(p => p.Reviews)
                    .FirstOrDefault(p => p.PackageId == id);

                if (package == null)
                {
                    return HttpNotFound();
                }

                if (package.PackageImages == null)
                {
                    package.PackageImages = new List<PackageImage>();
                }

                // Handle User Session for Booking Form
                User user = null;
                if (Session["UserId"] != null && int.TryParse(Session["UserId"].ToString(), out int userId))
                {
                    user = _context.Users
                        .AsNoTracking()
                        .FirstOrDefault(u => u.UserId == userId);
                }
                ViewBag.User = user;
                ViewBag.Destinations = _context.Destinations.AsNoTracking().ToList();

                // Load reviews with user info for the details page
                var reviews = _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.PackageId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();
                ViewBag.Reviews = reviews;

                return View(package);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Details: {ex.Message}");
                return HttpNotFound();
            }
        }

        // ── 4. PROCESS BOOKING ──
        // Bound to explicit fields matching the real [Booking] table:
        // BookingId, UserId, BookingDate, Status, TotalAmount, PackageId, Email,
        // DestinationId, SpecialRequest, CreatedDate.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBooking(int PackageId, DateTime BookingDate, int NumberOfTravelers,
            string Email, string SpecialRequest)
        {
            // 1. Must be logged in
            if (Session["UserId"] == null || !int.TryParse(Session["UserId"].ToString(), out int userId))
            {
                TempData["ErrorMessage"] = "Please log in to book a package.";
                return RedirectToAction("Index", "Login");
            }

            // 2. Package must exist (need Price + DestinationId)
            var package = _context.Packages.AsNoTracking().FirstOrDefault(p => p.PackageId == PackageId);
            if (package == null)
            {
                TempData["ErrorMessage"] = "The selected package could not be found.";
                return RedirectToAction("Index");
            }

            // 3. Basic server-side validation
            if (BookingDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Travel date cannot be in the past.";
                return RedirectToAction("Details", new { id = PackageId });
            }

            if (NumberOfTravelers < 1)
            {
                TempData["ErrorMessage"] = "Number of travelers must be at least 1.";
                return RedirectToAction("Details", new { id = PackageId });
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["ErrorMessage"] = "Please provide a contact email.";
                return RedirectToAction("Details", new { id = PackageId });
            }

            try
            {
                var booking = new Booking
                {
                    UserId = userId,
                    BookingDate = BookingDate,
                    Status = "Pending",
                    TotalAmount = package.Price * NumberOfTravelers,
                    PackageId = PackageId,
                    Email = Email,
                    DestinationId = package.DestinationId,
                    SpecialRequest = SpecialRequest,
                    CreatedDate = DateTime.Now
                };

                _context.Bookings.Add(booking);
                _context.SaveChanges();

                TempData["Message"] = "Booking successful! We'll email you a confirmation shortly.";
                return RedirectToAction("Details", new { id = PackageId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateBooking: {ex.Message}");
                TempData["ErrorMessage"] = "Something went wrong while processing your booking. Please try again.";
                return RedirectToAction("Details", new { id = PackageId });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}