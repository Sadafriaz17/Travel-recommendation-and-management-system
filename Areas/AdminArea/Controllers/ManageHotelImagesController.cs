using System;
using System.Data.Entity;
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
    public class ManageHotelImagesController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "HotelImages";
            var images = _context.HotelImages.Include(h => h.Hotel).ToList();
            return View(images);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "HotelImages";
            ViewBag.HotelList = _context.Hotels.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(HotelImage model, HttpPostedFileBase img)
        {
            model.CreatedDate = DateTime.Now;
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/hotels/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(Path.Combine(filePath, fileName));
                model.ImageUrl = "/uploads/hotels/" + fileName;
            }
            _context.HotelImages.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Hotel image added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "HotelImages";
            var image = _context.HotelImages.Find(id);
            if (image == null) return HttpNotFound();
            ViewBag.HotelList = _context.Hotels.ToList();
            return View(image);
        }

        [HttpPost]
        public ActionResult Edit(HotelImage model, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/hotels/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(Path.Combine(filePath, fileName));
                model.ImageUrl = "/uploads/hotels/" + fileName;
            }
            else
            {
                var existing = _context.HotelImages.Find(model.ImageId);
                if (existing != null) { model.ImageUrl = existing.ImageUrl; _context.Entry(existing).State = EntityState.Detached; }
            }
            _context.HotelImages.AddOrUpdate(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Hotel image updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var image = _context.HotelImages.Find(id);
            if (image != null)
            {
                _context.HotelImages.Remove(image);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Hotel image deleted!";
            }
            else { TempData["ErrorMessage"] = "Image not found!"; }
            return RedirectToAction("Index");
        }
    }
}