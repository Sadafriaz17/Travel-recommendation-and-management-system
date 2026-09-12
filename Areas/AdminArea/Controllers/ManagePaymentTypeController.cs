using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePaymentTypeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentTypes";
            var types = _context.PaymentTypes.ToList();
            return View(types);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentTypes";
            return View();
        }

        [HttpPost]
        public ActionResult Create(PaymentType model)
        {
            _context.PaymentTypes.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Payment type added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Payments";
            ViewBag.ActivePage = "PaymentTypes";
            var type = _context.PaymentTypes.Find(id);
            if (type == null) return HttpNotFound();
            return View(type);
        }

        [HttpPost]
        public ActionResult Edit(PaymentType model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Payment type updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var type = _context.PaymentTypes.Find(id);
            if (type != null)
            {
                _context.PaymentTypes.Remove(type);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment type deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
