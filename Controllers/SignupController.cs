using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class SignupController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();
        [HttpGet]
        public ActionResult Index()
        {


            return View();
        }
        [HttpPost]
        public ActionResult Index(User Model)
        {
            if (ModelState.IsValid)
            {
                var userexists = _context.Users.FirstOrDefault(u => u.Email == Model.Email);
                if (userexists != null)
                {
                    ViewBag.Message = "Email already registered.";

                    return View();
                }
                Model.UserTypeId = 2;
                Model.CreatedAt = DateTime.Now;
                _context.Users.Add(Model);
                _context.SaveChanges();
                ViewBag.Message = "Account created successfully.";
                return RedirectToAction("Index", "Login");

            }
            return View();
        }
    }
}