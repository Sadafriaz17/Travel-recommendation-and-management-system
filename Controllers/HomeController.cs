using System;
using System.Collections.Generic;
using System.Data.Entity;              // FIX: required for string-based .Include("...") on IQueryable<T>
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class HomeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // ── INDEX ──────────────────────────────────────────────────────────────
        public ActionResult Index()
        {
            return View();
        }

        // ── SEARCH ─────────────────────────────────────────────────────────────
        // GET: /Home/Search?query=...&budget=...&guests=...
        [HttpGet]
        public ActionResult Search(string query, string budget, string guests)
        {
            // Normalise inputs
            query = (query ?? "").Trim();
            budget = (budget ?? "").Trim();
            guests = (guests ?? "").Trim();

            // FIX: lower-cased copy for case-insensitive matching regardless of DB collation
            string q = query.ToLower();

            // Parse budget into a price range (null = no filter)
            decimal? priceMin = null;
            decimal? priceMax = null;
            switch (budget)
            {
                case "under500": priceMax = 500; break;
                case "500-2000": priceMin = 500; priceMax = 2000; break;
                case "2000-5000": priceMin = 2000; priceMax = 5000; break;
                case "5000plus": priceMin = 5000; break;
            }

            // Parse guest count (0 = no filter)
            int guestCount = 0;
            if (!string.IsNullOrEmpty(guests) && guests != "any")
            {
                if (guests == "6plus")
                    guestCount = 6;
                else
                    int.TryParse(guests, out guestCount);
            }

            // ── Query Destinations ─────────────────────────────────────────────
            var destQuery = _context.Destinations.AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                destQuery = destQuery.Where(d =>
                    d.Name.ToLower().Contains(q) ||
                    d.Location.ToLower().Contains(q) ||
                    (d.Description != null && d.Description.ToLower().Contains(q)));
            }

            if (priceMin.HasValue)
                destQuery = destQuery.Where(d => d.Price >= priceMin.Value);
            if (priceMax.HasValue)
                destQuery = destQuery.Where(d => d.Price <= priceMax.Value);

            var destinations = destQuery.Take(12).ToList();

            // ── Query Packages ─────────────────────────────────────────────────
            var pkgQuery = _context.Packages
                .Include("PackageImages")
                .Include("Destination")
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                pkgQuery = pkgQuery.Where(p =>
                    p.PackageName.ToLower().Contains(q) ||
                    (p.Description != null && p.Description.ToLower().Contains(q)));
            }

            if (priceMin.HasValue)
                pkgQuery = pkgQuery.Where(p => p.Price >= priceMin.Value);
            if (priceMax.HasValue)
                pkgQuery = pkgQuery.Where(p => p.Price <= priceMax.Value);

            if (guestCount > 0)
                pkgQuery = pkgQuery.Where(p => p.People >= guestCount);

            var packages = pkgQuery.Take(12).ToList();

            // ── Query Products ─────────────────────────────────────────────────
            var prodQuery = _context.Products
                .Include("ProductType")
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                prodQuery = prodQuery.Where(p =>
                    p.ProductName.ToLower().Contains(q) ||
                    (p.Description != null && p.Description.ToLower().Contains(q)));
            }

            var products = prodQuery.Take(12).ToList();

            // ── Pass to view ───────────────────────────────────────────────────
            ViewBag.SearchQuery = query;
            ViewBag.Budget = budget;
            ViewBag.Guests = guests;
            ViewBag.Destinations = destinations;
            ViewBag.Packages = packages;
            ViewBag.Products = products;

            int totalCount = destinations.Count + packages.Count + products.Count;
            ViewBag.TotalCount = totalCount;

            return View("SearchResults");
        }

        // ── OTHER PAGES ────────────────────────────────────────────────────────
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult Services()
        {
            ViewBag.Message = "Your application services page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}