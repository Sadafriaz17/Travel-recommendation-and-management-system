using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class LoginController : Controller
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
            var userexists = _context.Users.FirstOrDefault(u => u.Email == Model.Email && u.PasswordHash == Model.PasswordHash);
            if (userexists != null)
            {
                Session["UserId"] = userexists.UserId;
                Session["UserFullName"] = userexists.FullName;
               
                Session["UserTypeId"] = userexists.UserTypeId;
                
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Message = "Invalid email or password.";
                return View();
            }

        }
        public ActionResult Logout()
        {
            Session.Clear(); // Removes all session variables
            Session.Abandon(); // Ends the session
            return RedirectToAction("Index", "Home"); // Redirect to login page
        }

    }
}