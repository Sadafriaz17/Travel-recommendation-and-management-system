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
    public class ManageProductTypeController : Controller
    {

        Demo_DevDBEntities _context = new Demo_DevDBEntities();
        public ActionResult Index()
        {
            var productypelist = _context.ProductTypes.ToList();
            return View(productypelist);
        }
        [HttpGet]
        public ActionResult Create()
        {

            return View();
        }

        [HttpPost]
        public ActionResult Create(ProductType model, HttpPostedFileBase img)
        {
            try
            {



                // Handle image upload
                if (img != null && img.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/productTypes/");

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
                    model.ImageUrl = "/uploads/productTypes/" + fileName;
                }

                // Save to database
                _context.ProductTypes.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            // Reload dropdown in case of error or return
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();

            return RedirectToAction("Index");
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            var data = _context.ProductTypes.FirstOrDefault(x => x.ProductTypeId == id);
            return View(data);
        }
        [HttpPost]
        public ActionResult Edit(ProductType Model, HttpPostedFileBase img)
        {


            if (img != null && img.ContentLength > 0)
            {
                //name
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/productTypes/");
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

            _context.ProductTypes.AddOrUpdate(Model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("index");
        }
        public ActionResult Delete(int id)
        {
            var data = _context.ProductTypes.FirstOrDefault(x => x.ProductTypeId == id);
            if (data != null)
            {
                _context.ProductTypes.Remove(data);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Product not found!";
            }
            return RedirectToAction("Index");
        }


    }
}
