using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class VendorMarketPlaceController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            // All products with type + vendor for the main grid
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .ToList();

            // Vendors, with a live product count (not a stored/stale column)
            var vendors = _context.Vendors
                .Select(v => new VendorSummary
                {
                    VendorId = v.VendorId,
                    VendorName = v.VendorName,
                    Location = v.Location,
                    LogoIcon = v.LogoIcon,
                    Rating = v.Rating,
                    Description = v.Description,
                    ProductCount = v.Products.Count()
                })
                .OrderByDescending(v => v.Rating)
                .ToList();

            // Best sellers / trending strip — top 6 by review count
            var trending = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .OrderByDescending(p => p.ReviewCount)
                .Take(6)
                .ToList();

            ViewBag.ProductTypes = _context.ProductTypes.ToList();
            ViewBag.Vendors = vendors;
            ViewBag.TrendingProducts = trending;

            return View(products);
        }
    }

    // Lightweight projection for the vendor cards (keeps ProductCount live)
    public class VendorSummary
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string Location { get; set; }
        public string LogoIcon { get; set; }
        public decimal Rating { get; set; }
        public string Description { get; set; }
        public int ProductCount { get; set; }
    }
}