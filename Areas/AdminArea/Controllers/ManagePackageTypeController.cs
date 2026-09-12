using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePackageTypeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "PackageTypes";
            var packageTypes = _context.PackageTypes.ToList();
            return View(packageTypes);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "PackageTypes";
            return View();
        }

        [HttpPost]
        public ActionResult Create(PackageType model)
        {
            _context.PackageTypes.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Package type created successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu = "Packages";
            ViewBag.ActivePage = "PackageTypes";
            var packageType = _context.PackageTypes.Find(id);
            if (packageType == null) return HttpNotFound();
            return View(packageType);
        }

        [HttpPost]
        public ActionResult Edit(PackageType model)
        {
            _context.Entry(model).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Package type updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var packageType = _context.PackageTypes.Find(id);
            if (packageType != null)
            {
                _context.PackageTypes.Remove(packageType);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Package type deleted successfully!";
            }
            else { TempData["ErrorMessage"] = "Package type not found!"; }
            return RedirectToAction("Index");
        }
    }
}
