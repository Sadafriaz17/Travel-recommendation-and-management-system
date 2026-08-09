using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePaymentTypeController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // List all payment types
        public ActionResult Index()
        {
            var paymentTypes = _context.PaymentTypes.ToList();
            return View(paymentTypes);
        }

        // GET: Create
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        public ActionResult Create(PaymentType model)
        {
            model.CreatedDate = DateTime.Now;

            try
            {
                _context.PaymentTypes.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment type created successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // GET: Edit
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var data = _context.PaymentTypes.FirstOrDefault(x => x.PaymentTypeId == id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        // POST: Edit
        [HttpPost]
        public ActionResult Edit(PaymentType model)
        {
            model.CreatedDate = DateTime.Now;
            try
            {
                var existing = _context.PaymentTypes.FirstOrDefault(x => x.PaymentTypeId == model.PaymentTypeId);
                if (existing == null) return RedirectToAction("Index");

                existing.PaymentTypeName = model.PaymentTypeName;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment type updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Delete
        public ActionResult Delete(int id)
        {
            var data = _context.PaymentTypes.FirstOrDefault(x => x.PaymentTypeId == id);
            if (data != null)
            {
                _context.PaymentTypes.Remove(data);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Payment type deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
