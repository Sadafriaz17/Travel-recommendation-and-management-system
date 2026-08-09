using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageBookingsController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            var bookings = _context.Bookings.ToList();

            foreach (var booking in bookings)
            {
                if (string.IsNullOrEmpty(booking.Status))
                {
                    booking.Status = "Pending";
                }
            }

            return View(bookings);
        }

        public ActionResult Approve(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null)
            {
                booking.Status = "Approved";
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Reject
        public ActionResult Reject(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null)
            {
                booking.Status = "Rejected";
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Cancel
        public ActionResult Cancel(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null)
            {
                booking.Status = "Cancelled";
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Details View (to see Booking + BookingDetails)
        public ActionResult Details(int id)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == id);

            if (booking == null) return HttpNotFound();

            return View(booking);
        }
    }
}
    
