using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageProductTypeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "ProductTypes";
            var productTypes = _context.ProductTypes.ToList();
            return View(productTypes);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "ProductTypes";
            return View();
        }

        [HttpPost]
        public ActionResult Create(ProductType model)
        {
            _context.ProductTypes.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product type created successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "ProductTypes";
            var productType = _context.ProductTypes.Find(id);
            if (productType == null) return HttpNotFound();
            return View(productType);
        }

        [HttpPost]
        public ActionResult Edit(ProductType model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product type updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var productType = _context.ProductTypes.Find(id);
            if (productType != null)
            {
                _context.ProductTypes.Remove(productType);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product type deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
