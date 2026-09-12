using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageBlogsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Blogs";
            var blogs = _context.Blogs.ToList();
            return View(blogs);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Blogs";
            ViewBag.UserList = _context.Users.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Blog Model, HttpPostedFileBase img)
        {
            Model.CreatedDate = DateTime.Now;
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/Blogs/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(Path.Combine(filePath, fileName));
                Model.FeaturedImage = "/uploads/Blogs/" + fileName;
            }
            _context.Blogs.Add(Model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Blog post created successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Blogs";
            var blog = _context.Blogs.Find(id);
            if (blog == null) return HttpNotFound();
            ViewBag.UserList = _context.Users.ToList();
            return View(blog);
        }

        [HttpPost]
        public ActionResult Edit(Blog Model, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {
                string fileName = Path.GetFileName(img.FileName);
                string filePath = Server.MapPath("~/uploads/Blogs/");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                img.SaveAs(Path.Combine(filePath, fileName));
                Model.FeaturedImage = "/uploads/Blogs/" + fileName;
            }
            else
            {
                // Preserve existing image
                var existing = _context.Blogs.Find(Model.BlogId);
                if (existing != null) Model.FeaturedImage = existing.FeaturedImage;
                _context.Entry(existing ?? Model).State = System.Data.Entity.EntityState.Detached;
            }
            _context.Blogs.AddOrUpdate(Model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Blog post updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var data = _context.Blogs.FirstOrDefault(x => x.BlogId == id);
            if (data != null)
            {
                _context.Blogs.Remove(data);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Blog post deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Blog post not found!";
            }
            return RedirectToAction("Index");
        }
    }
}
