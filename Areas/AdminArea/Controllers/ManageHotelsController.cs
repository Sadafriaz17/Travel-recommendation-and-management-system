using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageHotelController : Controller
    {
        Demo_DevDBEntities _context = new Demo_DevDBEntities();



        public ActionResult Index()
        {
            // Include Destination to avoid null reference
            var packages = _context.Hotels
                                   .Include(p => p.HotelImages)
                                   .ToList();

            return View(packages);
        }


        public ActionResult Details(int id)
        {
            var Hotel = _context.Hotels.FirstOrDefault(x => x.PackageId == id);
            if (Hotel == null) return HttpNotFound();
            return View(Hotel);
        }

        [HttpGet]
        public ActionResult Create()
        {

            ViewBag.HotelList = _context.Hotels.ToList();
           
            return View();
        }

        [HttpPost]
        public ActionResult Create(Hotel Model, IEnumerable<HttpPostedFileBase> Images)
        {
            if (ModelState.IsValid)
            {
              
                _context.Hotels.Add(Model);
                _context.SaveChanges();



                TempData["SuccessMessage"] = "Hotel Added successfully!";
                return RedirectToAction("Index");
            }

            return View(Model);
        }

        [HttpGet]
        // GET: AdminArea/ManagePackages/Edit/5
        public ActionResult Edit(int id)
        {
            var Hotel = _context.Hotels.Find(id);
            if (Hotel == null)
            {
                return HttpNotFound();
            }

            // Populate package types and destinations
            ViewBag.HotelList = _context.Hotels.ToList();
           

            return View(Hotel);
        }


        [HttpPost]
        public ActionResult Edit(Hotel Model, IEnumerable<HttpPostedFileBase> NewImages)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(Model).State = EntityState.Modified;
              
                _context.SaveChanges();



                TempData["SuccessMessage"] = "Hotel updated successfully!";
                return RedirectToAction("Index");
            }

            return View(Model);
        }

        public ActionResult Delete(int id)
        {
            var data = _context.Hotels.FirstOrDefault(x => x.PackageId == id);
            if (data != null)
            {
                _context.Hotels.Remove(data);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Hotel deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Hotel not found!";
            }

            return RedirectToAction("Index");
        }
    }
}