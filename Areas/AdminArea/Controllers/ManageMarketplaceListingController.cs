using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageMarketplaceListingController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: Vendor list with their products
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Marketplace";
            var vendors = _context.Vendors
                .Include(v => v.Products)
                .Include("Products.ProductType")
                .OrderBy(v => v.VendorName)
                .ToList();
            return View(vendors);
        }

        // GET: Add new vendor
        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Marketplace";
            return View();
        }

        // POST: Save new vendor
        [HttpPost]
        public ActionResult Create(Vendor model)
        {
            if (string.IsNullOrWhiteSpace(model.VendorName))
            {
                ModelState.AddModelError("VendorName", "Vendor name is required.");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                if (model.Rating <= 0) model.Rating = 0;
                _context.Vendors.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Vendor added successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.ActiveMenu = "Marketplace";
            return View(model);
        }

        // GET: Edit vendor
        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Marketplace";
            var vendor = _context.Vendors.Find(id);
            if (vendor == null) return HttpNotFound();
            return View(vendor);
        }

        // POST: Save vendor edits
        [HttpPost]
        public ActionResult Edit(Vendor model)
        {
            if (string.IsNullOrWhiteSpace(model.VendorName))
            {
                ModelState.AddModelError("VendorName", "Vendor name is required.");
            }

            if (ModelState.IsValid)
            {
                var existing = _context.Vendors.Find(model.VendorId);
                if (existing != null)
                {
                    existing.VendorName  = model.VendorName;
                    existing.Location    = model.Location;
                    existing.LogoIcon    = model.LogoIcon;
                    existing.Rating      = model.Rating;
                    existing.Description = model.Description;
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Vendor updated successfully!";
                }
                return RedirectToAction("Index");
            }

            ViewBag.ActiveMenu = "Marketplace";
            return View(model);
        }

        // POST: Delete vendor
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var vendor = _context.Vendors
                .Include(v => v.Products)
                .FirstOrDefault(v => v.VendorId == id);

            if (vendor != null)
            {
                // Unlink products from this vendor before deleting
                foreach (var p in vendor.Products.ToList())
                {
                    p.VendorId = null;
                }
                _context.SaveChanges();

                _context.Vendors.Remove(vendor);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Vendor removed successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}