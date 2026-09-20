using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class DestinationController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            var destination = _context.Destinations.ToList();

            return View(destination);
        }

        public ActionResult HomepageDestination()
        {
            var destinations = _context.Destinations
                .OrderBy(d => Guid.NewGuid())
                .Take(6)
                .ToList();

            return View(destinations);
        }

        public ActionResult Details(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return RedirectToAction("Index", "Home");
            }

            // Optional: Url decoding if needed, but MVC usually handles it.
            // Replace dashes with spaces if you want to support slug-like URLs, e.g., "Southeast-Asia" -> "Southeast Asia"
            string searchRegion = region.Replace("-", " ");

            var destinations = _context.Destinations
                .Where(d => d.Location.Equals(searchRegion, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ViewBag.RegionName = searchRegion;
            return View(destinations);
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