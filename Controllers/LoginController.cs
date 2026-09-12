using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Helpers;

namespace TourwebsiteFYP.Controllers
{
    public class LoginController : Controller
    {
        private readonly Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string Email, string Password, bool RememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["LoginError"] = "Email and password are required.";
                return RedirectToAction("Index");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == Email.Trim().ToLower());

            if (user == null || !PasswordHelper.VerifyPassword(Password, user.PasswordHash))
            {
                TempData["LoginError"] = "Invalid email or password.";
                return RedirectToAction("Index");
            }

            Session["UserId"] = user.UserId;
            Session["UserName"] = user.FullName;
            Session["UserTypeId"] = user.UserTypeId;

            if (RememberMe)
            {
                Response.Cookies.Add(new System.Web.HttpCookie("RememberedEmail", user.Email)
                {
                    Expires = DateTime.Now.AddDays(30)
                });
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult GoogleLogin()
        {
            TempData["LoginError"] = "Google sign-in isn't configured yet.";
            return RedirectToAction("Index");
        }
    }
}