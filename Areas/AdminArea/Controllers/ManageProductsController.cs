using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageProductsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        //  Helper: returns the VendorId from session (null = not a vendor / no record).
        private int? GetSessionVendorId()
        {
            int typeId = 0;
            int.TryParse(Session["UserTypeId"]?.ToString(), out typeId);
            if (typeId != 3) return null;           // Only restrict for Vendor role

            object raw = Session["VendorId"];
            if (raw == null) return null;
            int vid;
            return int.TryParse(raw.ToString(), out vid) ? (int?)vid : null;
        }

        //  Helper: 403 redirect when a vendor tries to touch another vendor's product.
        private ActionResult Forbidden()
        {
            TempData["ErrorMessage"] = "You do not have permission to access that product.";
            return RedirectToAction("Index");
        }

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";

            int? vendorId = GetSessionVendorId();

            var query = _context.Products
                            .Include(p => p.ProductType)
                            .Include(p => p.Vendor)
                            .AsQueryable();

            // Vendors only see their own products.
            if (vendorId.HasValue)
            {
                query = query.Where(p => p.VendorId == vendorId.Value);
                ViewBag.IsVendorFiltered = true;
            }

            return View(query.ToList());
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";

            int? vendorId = GetSessionVendorId();
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();

            // Vendors only see their own vendor in the dropdown.
            ViewBag.VendorList = vendorId.HasValue
                ? _context.Vendors.Where(v => v.VendorId == vendorId.Value).ToList()
                : _context.Vendors.ToList();

            return View();
        }

        [HttpPost]
        public ActionResult Create(Product model,
            HttpPostedFileBase ImageFile,
            HttpPostedFileBase ImageFile2,
            HttpPostedFileBase ImageFile3,
            HttpPostedFileBase ImageFile4)
        {
            int? vendorId = GetSessionVendorId();

            // Force VendorId to the session vendor so they can't spoof a different one.
            if (vendorId.HasValue)
            {
                model.VendorId = vendorId.Value;
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                if (string.IsNullOrEmpty(model.StockStatus)) { model.StockStatus = "In Stock"; }

                SaveImage(ImageFile,  path => model.ImageUrl  = path);
                SaveImage(ImageFile2, path => model.ImageUrl2 = path);
                SaveImage(ImageFile3, path => model.ImageUrl3 = path);
                SaveImage(ImageFile4, path => model.ImageUrl4 = path);

                _context.Products.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = vendorId.HasValue
                ? _context.Vendors.Where(v => v.VendorId == vendorId.Value).ToList()
                : _context.Vendors.ToList();

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";

            var product = _context.Products.Find(id);
            if (product == null) { return HttpNotFound(); }

            // Vendors can only edit their own products.
            int? vendorId = GetSessionVendorId();
            if (vendorId.HasValue && product.VendorId != vendorId.Value) { return Forbidden(); }

            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = vendorId.HasValue
                ? _context.Vendors.Where(v => v.VendorId == vendorId.Value).ToList()
                : _context.Vendors.ToList();

            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(Product model,
            HttpPostedFileBase ImageFile,
            HttpPostedFileBase ImageFile2,
            HttpPostedFileBase ImageFile3,
            HttpPostedFileBase ImageFile4)
        {
            int? vendorId = GetSessionVendorId();

            if (ModelState.IsValid)
            {
                var existing = _context.Products.Find(model.ProductId);
                if (existing == null) { return HttpNotFound(); }

                // Vendors can only edit their own products.
                if (vendorId.HasValue && existing.VendorId != vendorId.Value) { return Forbidden(); }

                existing.ProductName   = model.ProductName;
                existing.ProductTypeId = model.ProductTypeId;
                existing.Description   = model.Description;
                existing.Price         = model.Price;
                existing.StockStatus   = model.StockStatus;
                existing.Rating        = model.Rating;
                existing.ReviewCount   = model.ReviewCount;

                // Vendors cannot reassign a product to a different vendor.
                if (!vendorId.HasValue) { existing.VendorId = model.VendorId; }

                SaveImage(ImageFile,  path => existing.ImageUrl  = path);
                SaveImage(ImageFile2, path => existing.ImageUrl2 = path);
                SaveImage(ImageFile3, path => existing.ImageUrl3 = path);
                SaveImage(ImageFile4, path => existing.ImageUrl4 = path);

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = vendorId.HasValue
                ? _context.Vendors.Where(v => v.VendorId == vendorId.Value).ToList()
                : _context.Vendors.ToList();

            return View(model);
        }

        public ActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) { return HttpNotFound(); }

            // Vendors can only delete their own products.
            int? vendorId = GetSessionVendorId();
            if (vendorId.HasValue && product.VendorId != vendorId.Value) { return Forbidden(); }

            _context.Products.Remove(product);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("Index");
        }

        //  Private helper to save an uploaded image file 
        private void SaveImage(HttpPostedFileBase file, Action<string> setter)
        {
            if (file != null && file.ContentLength > 0)
            {
                string path = "/Uploads/" + Path.GetFileName(file.FileName);
                file.SaveAs(Server.MapPath(path));
                setter(path);
            }
        }
    }
}