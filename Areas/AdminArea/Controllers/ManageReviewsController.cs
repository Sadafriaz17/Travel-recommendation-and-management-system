using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageReviewsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Reviews";
            ViewBag.ActivePage = "Reviews";
            var reviews = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Package)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            return View(reviews);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Reviews";
            ViewBag.ActivePage = "Reviews";
            ViewBag.UserList    = _context.Users.OrderBy(u => u.FullName).ToList();
            ViewBag.PackageList = _context.Packages.OrderBy(p => p.PackageName).ToList();
            ViewBag.ProductList = _context.Products.OrderBy(p => p.ProductName).ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Review model)
        {
            model.CreatedAt = DateTime.Now;
            _context.Reviews.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Review added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Reviews";
            ViewBag.ActivePage = "Reviews";
            var review = _context.Reviews.Find(id);
            if (review == null) return HttpNotFound();
            ViewBag.UserList    = _context.Users.OrderBy(u => u.FullName).ToList();
            ViewBag.PackageList = _context.Packages.OrderBy(p => p.PackageName).ToList();
            ViewBag.ProductList = _context.Products.OrderBy(p => p.ProductName).ToList();
            return View(review);
        }

        [HttpPost]
        public ActionResult Edit(Review model)
        {
            var existing = _context.Reviews.Find(model.ReviewId);
            if (existing != null)
            {
                existing.UserId    = model.UserId;
                existing.PackageId = model.PackageId;
                existing.ProductId = model.ProductId;
                existing.Rating    = model.Rating;
                existing.Comment   = model.Comment;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Review updated successfully!";
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Review deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Review not found!";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
