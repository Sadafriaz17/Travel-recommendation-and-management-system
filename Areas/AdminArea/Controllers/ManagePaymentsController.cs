using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePaymentsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentList";
            var payments = _context.Payments.Include(p => p.Booking).Include(p => p.PaymentType).ToList();
            return View(payments);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentList";
            ViewBag.BookingList = _context.Bookings.ToList();
            ViewBag.PaymentTypeList = _context.PaymentTypes.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Payment model)
        {
            model.PaymentDate = DateTime.Now;
            if(string.IsNullOrEmpty(model.Status)) model.Status = "Completed";
            _context.Payments.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Payment recorded successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentList";
            var payment = _context.Payments.Find(id);
            if (payment == null) return HttpNotFound();
            ViewBag.BookingList = _context.Bookings.ToList();
            ViewBag.PaymentTypeList = _context.PaymentTypes.ToList();
            return View(payment);
        }

        [HttpPost]
        public ActionResult Edit(Payment model)
        {
            var existing = _context.Payments.Find(model.PaymentId);
            if (existing != null)
            {
                existing.BookingId = model.BookingId;
                existing.PaymentTypeId = model.PaymentTypeId;
                existing.Amount = model.Amount;
                existing.Status = model.Status;
                existing.PaymentMethod = model.PaymentMethod;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment updated successfully!";
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var payment = _context.Payments.Find(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
