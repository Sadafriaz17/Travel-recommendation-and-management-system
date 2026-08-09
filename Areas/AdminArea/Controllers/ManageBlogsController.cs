using System;
using System.Collections.Generic;
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
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManageBlogs
        public ActionResult Index()
        {
            var blogs = _context.Blogs.ToList();
            return View(blogs);
        }

        // GET: AdminArea/ManageBlogs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminArea/ManageBlogs/Create
        [HttpPost]
        public ActionResult Create(Blog Model, HttpPostedFileBase img)
        {

            if (img != null && img.ContentLength > 0)
            {

                Model.CreatedDate = DateTime.Now;
                //name
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/Blogs/");
                //filepath exists
                if (!Directory.Exists(filePath))
                {

                    Directory.CreateDirectory(filePath);
                }
                //create full path
                string fullPath = Path.Combine(filePath, fileName);
                //image save 
                img.SaveAs(fullPath);
                //image save in db
                Model.FeaturedImage = "/uploads/Blogs/" + fileName;
            }
          
                _context.Blogs.Add(Model);
                _context.SaveChanges();
                return RedirectToAction("Index");
          
           
        }

        // GET: AdminArea/ManageBlogs/Edit/5
        public ActionResult Edit(int id)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null)
            {
                return HttpNotFound();
            }
            return View(blog);
        }

        // POST: AdminArea/ManageBlogs/Edit/5
        [HttpPost]
        public ActionResult Edit(Blog Model, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {

                Model.CreatedDate = DateTime.Now;
                //name
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/Blogs/");
                //filepath exists
                if (!Directory.Exists(filePath))
                {

                    Directory.CreateDirectory(filePath);
                }
                //create full path
                string fullPath = Path.Combine(filePath, fileName);
                //image save 
                img.SaveAs(fullPath);
                //image save in db
                Model.FeaturedImage = "/uploads/Blogs/" + fileName;
            }
            _context.Blogs.AddOrUpdate(Model);
            _context.SaveChanges();
            return RedirectToAction("Index");
          
          
        }

        // GET: AdminArea/ManageBlogs/Delete/5
        public ActionResult Delete(int id)
        {
            var data = _context.Blogs.FirstOrDefault(x => x.BlogId == id); ;
            _context.Blogs.Remove(data);
            _context.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";
            return RedirectToAction("index");
        }
    }
}
