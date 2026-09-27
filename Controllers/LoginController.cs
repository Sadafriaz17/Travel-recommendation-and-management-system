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

            // Google-only accounts have PasswordHash == null or empty.
            // Show a friendly message instead of the generic failure.
            if (user != null && string.IsNullOrEmpty(user.PasswordHash))
            {
                TempData["LoginError"] = "This account uses Google Sign-In - please use the \"Continue with Google\" button instead.";
                return RedirectToAction("Index");
            }

            if (user == null || !PasswordHelper.VerifyPassword(Password, user.PasswordHash))
            {
                TempData["LoginError"] = "Invalid email or password.";
                return RedirectToAction("Index");
            }

            // Establish the session (same three keys used throughout the app).
            Session["UserId"]     = user.UserId;
            Session["UserName"]   = user.FullName;
            Session["UserTypeId"] = user.UserTypeId;

            // For vendors: also persist their VendorId so every controller
            // can filter products without a fragile name-match lookup.
            if (user.UserTypeId == 3)
            {
                var vendor = _context.Vendors
                                 .FirstOrDefault(v => v.VendorName.Contains(user.FullName));
                Session["VendorId"] = vendor?.VendorId; // null if no vendor record yet
            }
            else
            {
                Session["VendorId"] = null;
            }

            if (RememberMe)
            {
                Response.Cookies.Add(new System.Web.HttpCookie("RememberedEmail", user.Email)
                {
                    Expires = DateTime.Now.AddDays(30)
                });
            }

            // Route to the role-specific dashboard instead of the public Home page.
            return RedirectToAction("Index", "DashboardRouter", new { area = "AdminArea" });
        }
    }
}
