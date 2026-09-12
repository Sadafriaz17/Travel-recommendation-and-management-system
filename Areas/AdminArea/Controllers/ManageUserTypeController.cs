using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageUserTypeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Users";
            ViewBag.ActivePage = "UserTypes";
            var types = _context.UserTypes.ToList();
            return View(types);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Users";
            ViewBag.ActivePage = "UserTypes";
            return View();
        }

        [HttpPost]
        public ActionResult Create(UserType model)
        {
            _context.UserTypes.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User role added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Users";
            ViewBag.ActivePage = "UserTypes";
            var type = _context.UserTypes.Find(id);
            if (type == null) return HttpNotFound();
            return View(type);
        }

        [HttpPost]
        public ActionResult Edit(UserType model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User role updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var type = _context.UserTypes.Find(id);
            if (type != null)
            {
                _context.UserTypes.Remove(type);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "User role deleted successfully!";
            }
            else { TempData["ErrorMessage"] = "Role not found!"; }
            return RedirectToAction("Index");
        }
    }
}
