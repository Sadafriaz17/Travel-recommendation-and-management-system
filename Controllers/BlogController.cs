using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class BlogController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // ── BLOG INDEX (Dynamic List) ──────────────────────────────────────
        public ActionResult Index()
        {
            var blogs = _context.Blogs
                .Include("User")
                .OrderByDescending(b => b.CreatedDate)
                .ToList();
            return View(blogs);
        }

        // ── BLOG DETAILS ───────────────────────────────────────────────────
        public ActionResult Details(int id)
        {
            var blog = _context.Blogs
                .Include("User")
                .FirstOrDefault(b => b.BlogId == id);
            
            if (blog == null) return HttpNotFound();

            // Find related posts (next/prev or random 2)
            ViewBag.RelatedBlogs = _context.Blogs
                .Where(b => b.BlogId != id)
                .OrderByDescending(b => b.CreatedDate)
                .Take(2)
                .ToList();

            return View(blog);
        }

        // ── HOMEPAGE BLOGS PARTIAL ─────────────────────────────────────────
        public ActionResult HomepageBlogs()
        {
            var blogs = _context.Blogs
                .Include("User")
                .OrderByDescending(b => b.CreatedDate)
                .Take(3)
                .ToList();
            return View(blogs);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
