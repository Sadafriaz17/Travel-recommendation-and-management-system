using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageGalleryController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Gallery";
            var gallery = _context.Galleries.ToList();
            return View(gallery);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Gallery";
            ViewBag.DestinationList = _context.Destinations.ToList();
            ViewBag.ProductList     = _context.Products.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Gallery model, HttpPostedFileBase img)
        {
            model.CreatedDate = DateTime.Now;
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/gallery/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(System.IO.Path.Combine(filePath, fileName));
                model.ImageUrl = "/uploads/gallery/" + fileName;
            }
            _context.Galleries.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Gallery item added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Gallery";
            var gallery = _context.Galleries.Find(id);
            if (gallery == null) return HttpNotFound();
            ViewBag.DestinationList = _context.Destinations.ToList();
            ViewBag.ProductList     = _context.Products.ToList();
            return View(gallery);
        }

        [HttpPost]
        public ActionResult Edit(Gallery model, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/gallery/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(System.IO.Path.Combine(filePath, fileName));
                model.ImageUrl = "/uploads/gallery/" + fileName;
            }
            else
            {
                var existing = _context.Galleries.Find(model.GalleryId);
                if (existing != null) model.ImageUrl = existing.ImageUrl;
            }
            _context.Galleries.AddOrUpdate(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Gallery item updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var gallery = _context.Galleries.Find(id);
            if (gallery != null)
            {
                _context.Galleries.Remove(gallery);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Gallery item deleted successfully!";
            }
            else { TempData["ErrorMessage"] = "Item not found!"; }
            return RedirectToAction("Index");
        }
    }
}
