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
                .OrderByDescending(d => d.DestinationId)
                .Take(6)
                .ToList();

            return View(destinations);
        }

        public ActionResult Details(int id)
        {
            var destination = _context.Destinations.FirstOrDefault(d => d.DestinationId == id);
            if (destination == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var packages = _context.Packages
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