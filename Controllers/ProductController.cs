using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Controllers
{
    public class ProductController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // All Products Page
        public ActionResult Index()
        {
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .OrderByDescending(p => p.ProductId)
                .ToList();

            ViewBag.ProductTypes = _context.ProductTypes.ToList();

            return View(products);
        }

        // Homepage Product Widget
        public ActionResult HomepageProducts()
        {
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .ToList();

            return View(products);
        }

        // Product Details Page
        public ActionResult Details(int id)
        {
            var product = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Current User
            User user = null;
            if (Session["UserId"] != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            }

            ViewBag.User = user;
            ViewBag.ProductTypes = _context.ProductTypes.ToList();

            return View(product);
        }

        // Related Products
        public ActionResult RelatedProducts(int productTypeId, int productId)
        {
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .Where(p => p.ProductTypeId == productTypeId &&
                            p.ProductId != productId)
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .ToList();

            return View(products);
        }

        // Product Categories
        public ActionResult Categories()
        {
            var categories = _context.ProductTypes.ToList();
            return View(categories);
        }

        // Products By Category
        public ActionResult CategoryProducts(int id)
        {
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .Where(p => p.ProductTypeId == id)
                .ToList();

            ViewBag.CategoryId = id;

            return View(products);
        }

        // Dropdown Categories Partial View
        public ActionResult CategoriesDropdown()
        {
            var categories = _context.ProductTypes.ToList();
            return PartialView(categories);
        }

        // Search Products
        public ActionResult Search(int ProductTypeId, string Search)
        {
            var products = _context.Products
                .Include("ProductType")
                .Include("Vendor")
                .Where(p =>
                    (string.IsNullOrEmpty(Search) ||
                     p.ProductName.ToLower().Contains(Search.ToLower()))
                    &&
                    (ProductTypeId == 0 ||
                     p.ProductTypeId == ProductTypeId))
                .ToList();

            return View(products);
        }
    }
}