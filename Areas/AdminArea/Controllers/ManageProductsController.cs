using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageProductsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            var products = _context.Products.Include(p => p.ProductType).ToList();
            return View(products);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Product model)
        {
            model.CreatedAt = DateTime.Now;
            if (string.IsNullOrEmpty(model.StockStatus)) model.StockStatus = "In Stock";
            _context.Products.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            var product = _context.Products.Find(id);
            if (product == null) return HttpNotFound();
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(Product model)
        {
            var existing = _context.Products.Find(model.ProductId);
            if (existing != null)
            {
                existing.ProductName = model.ProductName;
                existing.ProductTypeId = model.ProductTypeId;
                existing.Description = model.Description;
                existing.Price = model.Price;
                existing.StockStatus = model.StockStatus;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product updated successfully!";
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}