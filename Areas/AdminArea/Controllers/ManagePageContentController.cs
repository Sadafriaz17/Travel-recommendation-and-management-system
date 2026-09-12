using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePageContentController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "PageContent";
            var content = _context.PageContents.ToList();
            return View(content);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "PageContent";
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(PageContent model)
        {
            model.CreatedAt = DateTime.Now;
            _context.PageContents.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Page content added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "PageContent";
            var content = _context.PageContents.Find(id);
            if (content == null) return HttpNotFound();
            return View(content);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(PageContent model)
        {
            var existing = _context.PageContents.Find(model.Id);
            if (existing != null)
            {
                existing.PageName = model.PageName;
                existing.pageTitle = model.pageTitle;
                existing.pageSubtitle = model.pageSubtitle;
                existing.CreatedAt = DateTime.Now;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Page content updated successfully!";
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var content = _context.PageContents.Find(id);
            if (content != null)
            {
                _context.PageContents.Remove(content);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Page content deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}