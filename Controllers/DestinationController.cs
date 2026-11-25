using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class DestinationController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            var destination = _context.Destinations
                .AsNoTracking()
                .ToList();

            return View(destination);
        }

        public ActionResult HomepageDestination()
        {
            var destinations = _context.Destinations
                .AsNoTracking()
                .OrderByDescending(d => d.DestinationId)
                .Take(6)
                .ToList();

            return View(destinations);
        }

        public ActionResult Details(int id)
        {
            var destination = _context.Destinations
                .AsNoTracking()
                .FirstOrDefault(d => d.DestinationId == id);

            if (destination == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Load packages with all relations the view needs in a single query,
            // avoiding N×2 lazy-load roundtrips for PackageImages and PackageType.
            var packages = _context.Packages
                .AsNoTracking()
                .Include(p => p.PackageImages)
                .Include(p => p.PackageType)
                .Where(p => p.DestinationId == id)
                .OrderByDescending(p => p.CreatedDate)
                .ToList();

            ViewBag.Packages = packages;
            return View(destination);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}