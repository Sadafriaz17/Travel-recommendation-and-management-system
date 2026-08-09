using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePackageImagesController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            var images = _context.PackageImages.ToList();
            return View(images);
        }

        public ActionResult Details(int id)
        {
            var image = _context.PackageImages.FirstOrDefault(x => x.ImageId == id);
            if (image == null) return HttpNotFound();
            return View(image);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Packages = _context.Packages.ToList();
            return View();
        }

        [HttpPost]
       
        public ActionResult Create(PackageImage model, IEnumerable<HttpPostedFileBase> files)
        {
            if (ModelState.IsValid)
            {
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string path = "/Uploads/" + Path.GetFileName(file.FileName);
                            file.SaveAs(Server.MapPath(path));

                            // Create a new PackageImage record for each file
                            var image = new PackageImage
                            {
                                PackageId = model.PackageId, // ensure PackageId is set
                                ImageUrl = path
                            };

                            _context.PackageImages.Add(image);
                        }
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Images added successfully!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Packages = _context.Packages.ToList();
            return View(model);
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            var image = _context.PackageImages.Find(id);
            if (image == null) return HttpNotFound();

            ViewBag.Packages = _context.Packages.ToList();
            return View(image);
        }

        [HttpPost]
        public ActionResult Edit(PackageImage model, IEnumerable<HttpPostedFileBase> files)
        {
            if (ModelState.IsValid)
            {
                var img = _context.PackageImages.Find(model.ImageId);
                if (img == null) return HttpNotFound();

                img.PackageId = model.PackageId;

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string path = "/Uploads/" + Path.GetFileName(file.FileName);
                            file.SaveAs(Server.MapPath(path));

                            // Update the existing record with the last uploaded file 
                            // OR create new records if you want to keep multiple images
                            img.ImageUrl = path;
                        }
                    }
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Image(s) updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Packages = _context.Packages.ToList();
            return View(model);
        }

        public ActionResult Delete(int id)
        {
            var image = _context.PackageImages.FirstOrDefault(x => x.ImageId == id);
            if (image != null)
            {
                _context.PackageImages.Remove(image);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Image deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Image not found!";
            }

            return View();
        }
    }
}
