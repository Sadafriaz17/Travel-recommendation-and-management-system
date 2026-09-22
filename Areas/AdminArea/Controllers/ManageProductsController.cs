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

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            var products = _context.Products.Include(p => p.ProductType).Include(p => p.Vendor).ToList();
            return View(products);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = _context.Vendors.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Product model, HttpPostedFileBase ImageFile, HttpPostedFileBase ImageFile2, HttpPostedFileBase ImageFile3, HttpPostedFileBase ImageFile4)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                if (string.IsNullOrEmpty(model.StockStatus)) model.StockStatus = "In Stock";

                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string path = "/Uploads/" + Path.GetFileName(ImageFile.FileName);
                    ImageFile.SaveAs(Server.MapPath(path));
                    model.ImageUrl = path;
                }
                
                if (ImageFile2 != null && ImageFile2.ContentLength > 0)
                {
                    string path = "/Uploads/" + Path.GetFileName(ImageFile2.FileName);
                    ImageFile2.SaveAs(Server.MapPath(path));
                    model.ImageUrl2 = path;
                }
                
                if (ImageFile3 != null && ImageFile3.ContentLength > 0)
                {
                    string path = "/Uploads/" + Path.GetFileName(ImageFile3.FileName);
                    ImageFile3.SaveAs(Server.MapPath(path));
                    model.ImageUrl3 = path;
                }
                
                if (ImageFile4 != null && ImageFile4.ContentLength > 0)
                {
                    string path = "/Uploads/" + Path.GetFileName(ImageFile4.FileName);
                    ImageFile4.SaveAs(Server.MapPath(path));
                    model.ImageUrl4 = path;
                }

                _context.Products.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction("Index");
            }
            
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = _context.Vendors.ToList();
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            var product = _context.Products.Find(id);
            if (product == null) return HttpNotFound();
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = _context.Vendors.ToList();
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(Product model, HttpPostedFileBase ImageFile, HttpPostedFileBase ImageFile2, HttpPostedFileBase ImageFile3, HttpPostedFileBase ImageFile4)
        {
            if (ModelState.IsValid)
            {
                var existing = _context.Products.Find(model.ProductId);
                if (existing != null)
                {
                    existing.ProductName = model.ProductName;
                    existing.ProductTypeId = model.ProductTypeId;
                    existing.Description = model.Description;
                    existing.Price = model.Price;
                    existing.StockStatus = model.StockStatus;
                    existing.VendorId = model.VendorId;
                    existing.Rating = model.Rating;
                    existing.ReviewCount = model.ReviewCount;

                    if (ImageFile != null && ImageFile.ContentLength > 0)
                    {
                        string path = "/Uploads/" + Path.GetFileName(ImageFile.FileName);
                        ImageFile.SaveAs(Server.MapPath(path));
                        existing.ImageUrl = path;
                    }
                    if (ImageFile2 != null && ImageFile2.ContentLength > 0)
                    {
                        string path = "/Uploads/" + Path.GetFileName(ImageFile2.FileName);
                        ImageFile2.SaveAs(Server.MapPath(path));
                        existing.ImageUrl2 = path;
                    }
                    if (ImageFile3 != null && ImageFile3.ContentLength > 0)
                    {
                        string path = "/Uploads/" + Path.GetFileName(ImageFile3.FileName);
                        ImageFile3.SaveAs(Server.MapPath(path));
                        existing.ImageUrl3 = path;
                    }
                    if (ImageFile4 != null && ImageFile4.ContentLength > 0)
                    {
                        string path = "/Uploads/" + Path.GetFileName(ImageFile4.FileName);
                        ImageFile4.SaveAs(Server.MapPath(path));
                        existing.ImageUrl4 = path;
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction("Index");
                }
                return HttpNotFound();
            }
            
            ViewBag.ActiveMenu = "Products";
            ViewBag.ActivePage = "Products";
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            ViewBag.VendorList = _context.Vendors.ToList();
            return View(model);
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