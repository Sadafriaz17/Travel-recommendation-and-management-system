using System;
using System.Collections.Generic;
using System.Data.Entity;
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
    public class ManagePackagesController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

   

public ActionResult Index()
    {
        // Include Destination to avoid null reference
        var packages = _context.Packages
                               .Include(p => p.Destination)
                               .ToList();

        return View(packages);
    }


    public ActionResult Details(int id)
        {
            var package = _context.Packages.FirstOrDefault(x => x.PackageId == id);
            if (package == null) return HttpNotFound();
            return View(package);
        }

        [HttpGet]
        public ActionResult Create()
        {

            ViewBag.PackageTypes = _context.PackageTypes.ToList();
            ViewBag.Destinations = _context.Destinations.ToList();
           
            return View();
        }

        [HttpPost]
        public ActionResult Create(Package package, IEnumerable<HttpPostedFileBase> Images)
        {
            if (ModelState.IsValid)
            {
                package.CreatedDate = DateTime.Now;
                _context.Packages.Add(package);
                _context.SaveChanges();

             

                TempData["SuccessMessage"] = "Package created successfully!";
                return RedirectToAction("Index");
            }

            return View(package);
        }

        [HttpGet]
        // GET: AdminArea/ManagePackages/Edit/5
        public ActionResult Edit(int id)
        {
            var package = _context.Packages.Find(id);
            if (package == null)
            {
                return HttpNotFound();
            }

            // Populate package types and destinations
            ViewBag.PackageTypes = _context.PackageTypes.ToList();
            ViewBag.Destinations = _context.Destinations.ToList();

            return View(package);
        }


        [HttpPost]
        public ActionResult Edit(Package package, IEnumerable<HttpPostedFileBase> NewImages)
        {
            if (ModelState.IsValid)
            {
                var existingPackage = _context.Packages.Find(package.PackageId);
                if (existingPackage != null)
                {
                    existingPackage.PackageName = package.PackageName;
                    existingPackage.PackageTypeId = package.PackageTypeId;
                    existingPackage.DestinationId = package.DestinationId;
                    existingPackage.Price = package.Price;
                    existingPackage.DurationDays = package.DurationDays;
                    existingPackage.People = package.People;
                    existingPackage.Description = package.Description;
                    existingPackage.UpdatedDate = DateTime.Now;

                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Package updated successfully!";
                    return RedirectToAction("Index");
                }
                return HttpNotFound();
            }

            // Populate package types and destinations for view if validation fails
            ViewBag.PackageTypes = _context.PackageTypes.ToList();
            ViewBag.Destinations = _context.Destinations.ToList();

            return View(package);
        }

        public ActionResult Delete(int id)
        {
            var package = _context.Packages.FirstOrDefault(x => x.PackageId == id);
            if (package != null)
            {
                _context.Packages.Remove(package);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Package deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Package not found!";
            }

            return RedirectToAction("Index");
        }
    }
}
