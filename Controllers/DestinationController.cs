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