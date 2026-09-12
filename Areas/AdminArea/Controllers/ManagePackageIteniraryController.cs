using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePackageIteniraryController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "Itinerary";
            var itineraries = _context.PackageItineraries.Include(i => i.Package).ToList();
            return View(itineraries);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "Itinerary";
            ViewBag.PackageList = _context.Packages.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(PackageItinerary model)
        {
            _context.PackageItineraries.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Itinerary added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "Itinerary";
            var itinerary = _context.PackageItineraries.Find(id);
            if (itinerary == null) return HttpNotFound();
            ViewBag.PackageList = _context.Packages.ToList();
            return View(itinerary);
        }

        [HttpPost]
        public ActionResult Edit(PackageItinerary model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Itinerary updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var itinerary = _context.PackageItineraries.Find(id);
            if (itinerary != null)
            {
                _context.PackageItineraries.Remove(itinerary);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Itinerary deleted successfully!";
            }
            else { TempData["ErrorMessage"] = "Itinerary not found!"; }
            return RedirectToAction("Index");
        }
    }
}