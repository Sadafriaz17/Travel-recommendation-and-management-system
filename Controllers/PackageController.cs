using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Controllers
{
    public class PackageController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();
        public ActionResult Index()
        {
            var packages = _context.Packages.ToList();
            return View(packages);
        }

        public ActionResult Homepagepackage()
        {
            var Packages = _context.Packages
                 .OrderBy(d => Guid.NewGuid())
                 .Take(3)
                 .ToList();

            return View(Packages);
        }


        public ActionResult Details(int id)
        {
            // Fetch package with related Destination and Images
            var package = _context.Packages
                
                //.Include("PackageItineraries")  for know leave them commented and after completing crud on these then add hotel fligh and itenirary data continue this. 
                //.Include("Flights")
                //.Include("Hotels")
                //.Include("HotelImages")
             .Include("Destination")
             .Include("PackageImages")   
                .FirstOrDefault(p => p.PackageId == id);

            if (package == null)
            {
                return HttpNotFound(); // handle invalid id
            }

            // Get current logged-in user from Session
            User user = null;
            if (Session["UserId"] != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            }

            // Fetch destinations & related packages for dropdown/other use
            ViewBag.User = user;
            ViewBag.Destinations = _context.Destinations.ToList();
            ViewBag.Packages = _context.Packages.ToList();

            return View(package);
        }

        [SessionAuthFilter]
        [HttpPost]
        public ActionResult CreateBooking(Booking booking)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Login"); // force login
            }

            if (!ModelState.IsValid)
            {
                // If invalid data, reload details page
                return RedirectToAction("Details", new { id = booking.PackageId });
            }

            // Set UserId from Session
            booking.UserId = Convert.ToInt32(Session["UserId"]);

            // Use user-selected booking date OR keep existing if you want
            if (booking.BookingDate == default(DateTime))
            {
                booking.BookingDate = DateTime.Now;
            }

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            TempData["Message"] = "Your booking has been submitted successfully!";
            return RedirectToAction("Details", new { id = booking.PackageId });
        }

    }
}
