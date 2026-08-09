using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageUsersController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            var listofData = _context.Users.ToList();
            return View(listofData);
        }
        [HttpGet]
        public ActionResult Create()
        {
            var usertype = _context.UserTypes.ToList();
            ViewBag.usertypelist = usertype;
            return View();
        }

        [HttpPost]
        public ActionResult Create(User model)
        {
            try
            {
                model.CreatedAt = DateTime.Now;
                _context.Users.Add(model);
                _context.SaveChanges();

                ViewBag.Message = "Data Insert Successfully";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }


            ViewBag.usertypeList = _context.UserTypes.ToList();


            return View(model);
        }




        [HttpGet]
        public ActionResult Edit(int id)
        {
            var data = _context.Users.FirstOrDefault(x => x.UserId == id);
            ViewBag.usertypeList = _context.UserTypes.ToList();
            return View(data);
        }
        [HttpPost]
        public ActionResult Edit(User Model)
        {

            _context.Users.AddOrUpdate(Model);
            _context.SaveChanges();
            return RedirectToAction("index");
        }
        public ActionResult Delete(int id)
        {
            var data = _context.Users.FirstOrDefault(x => x.UserId == id);
            _context.Users.Remove(data);
            _context.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";
            return RedirectToAction("index");

        }
    }
}