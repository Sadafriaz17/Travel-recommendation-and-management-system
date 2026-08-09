using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageDestinationController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManageDestination
        public ActionResult Index()
        {
            var destinations = _context.Destinations.ToList();
            return View(destinations);
        }

        // GET: Create
        public ActionResult Create()
        {
            ViewBag.Packagelist = _context.PackageTypes.ToList();
            ViewBag.DestinationList = _context.Destinations.ToList();
            return View();
        }

        // POST: Create
        [HttpPost]
       
        public ActionResult Create(Destination Model, HttpPostedFileBase img)
        {
            Model.CreatedDate= DateTime.Now;
            if (img != null && img.ContentLength > 0)
            {
                //name  
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/destinations/");
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
                Model.Image = "/uploads/destinations/" + fileName;
            }
            _context.Destinations.Add(Model);
                _context.SaveChanges();
                return RedirectToAction("Index");
           
        }

        // GET: Edit
        public ActionResult Edit(int id)
        {
            // Load Package Types for dropdown
            ViewBag.PackageTypeList = _context.PackageTypes.ToList();

            // Find the specific destination to edit
            var destination = _context.Destinations.Find(id);

            if (destination == null)
            {
                return HttpNotFound();
            }

            return View(destination);
        }


        // POST: Edit
        [HttpPost]
      
        public ActionResult Edit(Destination Model, HttpPostedFileBase img)
        {
            Model.CreatedDate = DateTime.Now;
            if (img != null && img.ContentLength > 0)
            {
                //name  
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/destinations/");
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
                Model.Image = "/uploads/destinations/" + fileName;
            }
            _context.Destinations.AddOrUpdate(Model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            
        }

        // GET: Delete
        public ActionResult Delete(int id)
        {
            var destination = _context.Destinations.Find(id);
            _context.Destinations.Remove(destination);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

       
    }
}
