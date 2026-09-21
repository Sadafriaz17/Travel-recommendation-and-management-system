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

            // Clarification #5: Google-only accounts have PasswordHash == null.
            // Show a friendly message instead of letting VerifyPassword return false
            // (which would show the generic "Invalid email or password" message).
            if (user != null && user.PasswordHash == null)
            {
                TempData["LoginError"] = "This account uses Google Sign-In - please use the \"Continue with Google\" button instead.";
                return RedirectToAction("Index");
            }

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

    }
}
