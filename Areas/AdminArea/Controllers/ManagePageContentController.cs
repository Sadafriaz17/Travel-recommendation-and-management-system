using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class    ManagePageContentController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManagePageContent
        public ActionResult Index()
        {
            var contents = _context.PageContents.ToList();
            return View(contents);
        }

        // GET: Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(PageContent model)
        {
            model.CreatedAt = DateTime.Now;
            if (ModelState.IsValid)
            {
                _context.PageContents.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: Edit
        public ActionResult Edit(int id)
        {

            var content = _context.PageContents.Find(id);
            if (content == null) return HttpNotFound();
            return View(content);
        }

        [HttpPost]
        public ActionResult Edit(PageContent model)
        {
            if (ModelState.IsValid)
            {
                var existing = _context.PageContents.Find(model.Id);
                if (existing == null) return HttpNotFound();

                existing.PageName = model.PageName;
                existing.pageTitle = model.pageTitle;
                existing.pageSubtitle = model.pageSubtitle;
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: Delete
        public ActionResult Delete(int id)
        {
            var data = _context.PageContents.Find(id);
            _context.PageContents.Remove(data);
            _context.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";
            return RedirectToAction("index");
        }

       
    }
}