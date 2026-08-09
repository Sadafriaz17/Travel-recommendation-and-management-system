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
    public class ManageProductsController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();
        public ActionResult Index()
        {
            var listofData = _context.Products.ToList();
            return View(listofData);
        }
        [HttpGet]
        public ActionResult Create()
        {

            //gets all the product types from the database
            var proType = _context.ProductTypes.ToList();
            //saves its value from viewbag
            ViewBag.ProductTypeList = proType;
            return View();
        }
        [HttpPost]

        public ActionResult Create(Product model, HttpPostedFileBase img, HttpPostedFileBase img2, HttpPostedFileBase img3, HttpPostedFileBase img4)
        {
            try
            {
                // Set current datetime
                model.CreatedAt = DateTime.Now;

                // Handle image upload
                if (img != null && img.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/products/");

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
                    model.ImageUrl = "/uploads/products/" + fileName;
                }
                if (img2 != null && img2.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img2.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/products/");

                    // Create folder if it doesn't exist
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    // Full path with filename
                    string fullPath = Path.Combine(filePath, fileName);

                    // Save file
                    img2.SaveAs(fullPath);

                    // Save relative path to the model
                    model.ImageUrl2 = "/uploads/products/" + fileName;
                }
                if (img3 != null && img3.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img3.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/products/");

                    // Create folder if it doesn't exist
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    // Full path with filename
                    string fullPath = Path.Combine(filePath, fileName);

                    // Save file
                    img3.SaveAs(fullPath);

                    // Save relative path to the model
                    model.ImageUrl3 = "/uploads/products/" + fileName;
                }
                if (img4 != null && img4.ContentLength > 0)
                {
                    // Get image file name
                    string fileName = Path.GetFileName(img4.FileName);

                    // Create a folder path to store the image
                    string filePath = Server.MapPath("~/uploads/products/");

                    // Create folder if it doesn't exist
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    // Full path with filename
                    string fullPath = Path.Combine(filePath, fileName);

                    // Save file
                    img4.SaveAs(fullPath);

                    // Save relative path to the model
                    model.ImageUrl4 = "/uploads/products/" + fileName;
                }

                // Save to database
                _context.Products.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Product inserted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            ViewBag.Producttypelist = _context.ProductTypes.ToList();
            return RedirectToAction("Index");



        }

        [HttpGet]
        public ActionResult Edit(int id)
        {


            var data = _context.Products.Where(x => x.ProductId == id).FirstOrDefault();
            ViewBag.ProductTypeList = _context.ProductTypes.ToList();
            return View(data);
        }
        [HttpPost]
        public ActionResult Edit(Product model, HttpPostedFileBase img, HttpPostedFileBase img2, HttpPostedFileBase img3, HttpPostedFileBase img4)
        {


            if (img != null && img.ContentLength > 0)
            {
                // Get image file name
                string fileName = Path.GetFileName(img.FileName);

                // Create a folder path to store the image
                string filePath = Server.MapPath("~/uploads/Products/");

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
                model.ImageUrl = "/uploads/Products/" + fileName;
            }
            if (img2 != null && img2.ContentLength > 0)
            {
                // Get image file name
                string fileName = Path.GetFileName(img2.FileName);

                // Create a folder path to store the image
                string filePath = Server.MapPath("~/uploads/Products/");

                // Create folder if it doesn't exist
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                // Full path with filename
                string fullPath = Path.Combine(filePath, fileName);

                // Save file
                img2.SaveAs(fullPath);

                // Save relative path to the model
                model.ImageUrl2 = "/uploads/Products/" + fileName;
            }
            if (img3 != null && img3.ContentLength > 0)
            {
                // Get image file name
                string fileName = Path.GetFileName(img3.FileName);

                // Create a folder path to store the image
                string filePath = Server.MapPath("~/uploads/Products/");

                // Create folder if it doesn't exist
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                // Full path with filename
                string fullPath = Path.Combine(filePath, fileName);

                // Save file
                img3.SaveAs(fullPath);

                // Save relative path to the model
                model.ImageUrl3 = "/uploads/Products/" + fileName;
            }
            if (img4 != null && img4.ContentLength > 0)
            {
                // Get image file name
                string fileName = Path.GetFileName(img4.FileName);

                // Create a folder path to store the image
                string filePath = Server.MapPath("~/uploads/Products/");

                // Create folder if it doesn't exist
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                // Full path with filename
                string fullPath = Path.Combine(filePath, fileName);

                // Save file
                img4.SaveAs(fullPath);

                // Save relative path to the model
                model.ImageUrl4 = "/uploads/Products/" + fileName;


            }
            _context.Products.AddOrUpdate(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("Index");
        }


        public ActionResult Delete(int id)
        {
            var data = _context.Products.FirstOrDefault(x => x.ProductId == id);
            _context.Products.Remove(data);
            _context.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";
            return RedirectToAction("index");
        }

    }
}