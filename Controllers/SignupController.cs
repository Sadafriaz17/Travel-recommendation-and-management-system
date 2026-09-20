using System;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Helpers;

namespace TourwebsiteFYP.Controllers
{
    public class SignupController : Controller
    {
        private readonly Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string FullName, string Email, string Password, string ConfirmPassword, bool AcceptTerms = false)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["SignupError"] = "All fields are required.";
                return RedirectToAction("Index");
            }

            if (Password != ConfirmPassword)
            {
                TempData["SignupError"] = "Passwords do not match.";
                return RedirectToAction("Index");
            }

            if (!AcceptTerms)
            {
                TempData["SignupError"] = "You must accept the Terms of Service.";
                return RedirectToAction("Index");
            }

            if (_context.Users.Any(u => u.Email.ToLower() == Email.Trim().ToLower()))
            {
                TempData["SignupError"] = "An account with this email already exists.";
                return RedirectToAction("Index");
            }

            var defaultUserType = _context.UserTypes.FirstOrDefault(t => t.TypeName == "Customer")
                                   ?? _context.UserTypes.FirstOrDefault();

            if (defaultUserType == null)
            {
                TempData["SignupError"] = "Signup is temporarily unavailable — no user type is configured.";
                return RedirectToAction("Index");
            }

            var newUser = new User
            {
                FullName = FullName.Trim(),
                Email = Email.Trim().ToLower(),
                PasswordHash = PasswordHelper.HashPassword(Password),
                UserTypeId = defaultUserType.UserTypeId,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            Session["UserId"] = newUser.UserId;
            Session["UserName"] = newUser.FullName;
            Session["UserTypeId"] = newUser.UserTypeId;

            return RedirectToAction("Index", "Home");
        }

    }
}