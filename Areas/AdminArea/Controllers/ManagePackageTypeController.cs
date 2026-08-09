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
    public class ManagePackageTypeController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();
        public ActionResult Index()
        {
            var packagetypelist = _context.PackageTypes.ToList();
            return View(packagetypelist);
        }
        [HttpGet]
        public ActionResult Create()
        {
          
            return View();
        }

        [HttpPost]
        public ActionResult Create(PackageType model, HttpPostedFileBase img)
        {
            try
            {



                // Handle image upload
                if (img != null && img.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/packageTypes/");

                    // Create folder if it doesn't exist
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    // Full path with filename
                    string fullPath = Path.Combine(filePath, fileName);

                    // Save file
                    img.SaveAs(fullPath);

                    // Save relative path to the model
                    model.ImageUrl = "/uploads/packageTypes/" + fileName;
                }

                // Save to database
                _context.PackageTypes.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Package deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            // Reload dropdown in case of error or return
            ViewBag.PackageTypeList = _context.PackageTypes.ToList();

            return RedirectToAction("Index");
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            var data = _context.PackageTypes.FirstOrDefault(x => x.PackageTypeId == id);
            return View(data);
        }
        [HttpPost]
        public ActionResult Edit(PackageType Model, HttpPostedFileBase img)
        {


            if (img != null && img.ContentLength > 0)
            {
                //name
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/packageTypes/");
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
                Model.ImageUrl = "/uploads/packageTypes/" + fileName;
            }

            _context.PackageTypes.AddOrUpdate(Model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Package updated successfully!";
            return RedirectToAction("index");
        }
        public ActionResult Delete(int id)
        {
            var data = _context.PackageTypes.FirstOrDefault(x => x.PackageTypeId == id);
            if(data != null){
                _context.PackageTypes.Remove(data);
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
