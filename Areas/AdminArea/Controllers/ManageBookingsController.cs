using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageBookingsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Bookings";
            var bookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Package)
                .Include(b => b.Destination)
                .ToList();
            return View(bookings);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Bookings";
            ViewBag.UserList        = _context.Users.ToList();
            ViewBag.PackageList     = _context.Packages.ToList();
            ViewBag.DestinationList = _context.Destinations.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Booking model)
        {
            model.CreatedDate = DateTime.Now;
            if (string.IsNullOrEmpty(model.Status)) model.Status = "Pending";
            _context.Bookings.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Booking created successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Bookings";
            var booking = _context.Bookings.Find(id);
            if (booking == null) return HttpNotFound();
            ViewBag.UserList        = _context.Users.ToList();
            ViewBag.PackageList     = _context.Packages.ToList();
            ViewBag.DestinationList = _context.Destinations.ToList();
            return View(booking);
        }

        [HttpPost]
        public ActionResult Edit(Booking model)
        {
            var existing = _context.Bookings.Find(model.BookingId);
            if (existing == null) return HttpNotFound();
            existing.UserId        = model.UserId;
            existing.PackageId     = model.PackageId;
            existing.DestinationId = model.DestinationId;
            existing.Status        = model.Status;
            existing.TotalAmount   = model.TotalAmount;
            existing.Email         = model.Email;
            existing.SpecialRequest= model.SpecialRequest;
            existing.BookingDate   = model.BookingDate;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Booking updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Approve(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null) { booking.Status = "Approved"; _context.SaveChanges(); TempData["SuccessMessage"] = "Booking approved."; }
            return RedirectToAction("Index");
        }

        public ActionResult Reject(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null) { booking.Status = "Rejected"; _context.SaveChanges(); TempData["SuccessMessage"] = "Booking rejected."; }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Booking deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Booking not found!";
            }
            return RedirectToAction("Index");
        }
    }
}
