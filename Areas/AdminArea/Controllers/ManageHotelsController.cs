using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageHotelsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "Hotels";
            var hotels = _context.Hotels.Include(h => h.Package).ToList();
            return View(hotels);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "Hotels";
            ViewBag.PackageList = _context.Packages.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Hotel model)
        {
            _context.Hotels.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Hotel added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Hotels";
            ViewBag.ActivePage = "Hotels";
            var hotel = _context.Hotels.Find(id);
            if (hotel == null) return HttpNotFound();
            ViewBag.PackageList = _context.Packages.ToList();
            return View(hotel);
        }

        [HttpPost]
        public ActionResult Edit(Hotel model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Hotel updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var hotel = _context.Hotels.Find(id);
            if (hotel != null)
            {
                _context.Hotels.Remove(hotel);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Hotel deleted successfully!";
            }
            else { TempData["ErrorMessage"] = "Hotel not found!"; }
            return RedirectToAction("Index");
        }
    }
}