using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    /// <summary>
    /// Google sign-in and sign-up for the user-facing site.
    /// Uses the same Session keys as LoginController (UserId, UserName, UserTypeId, VendorId).
    /// </summary>
    public class GoogleAuthController : Controller
    {
        private const string GoogleProvider = "Google";

        private readonly Demo_DevDBEntities _context = new Demo_DevDBEntities();

        private static string GetClaim(ClaimsIdentity identity, string claimType)
        {
            return identity?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        private IAuthenticationManager AuthManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        private ActionResult Fail(string message)
        {
            TempData["LoginError"] = message;
            AuthManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            return RedirectToAction("Index", "Login");
        }

        // STEP 1: send the browser to Google.
        // The provider value from the URL is ignored. Only Google is allowed.
        [AllowAnonymous]
        public ActionResult ExternalLogin(string provider)
        {
            if (!Startup.GoogleConfigured)
            {
                return Fail("Google sign-in is not set up yet. Please use your email and password.");
            }

            Startup.LastOwinProblem = null;

            string redirectUri = Url.Action("ExternalLoginCallback", "GoogleAuth", null, Request.Url.Scheme);

            AuthManager.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, GoogleProvider);

            // A 401 result lets OWIN take over and redirect to Google.
            return new HttpUnauthorizedResult();
        }

        // STEP 2: Google sends the user back here.
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback()
        {
            ExternalLoginInfo loginInfo = await AuthManager.GetExternalLoginInfoAsync();

            if (loginInfo == null || loginInfo.Login.LoginProvider != GoogleProvider)
            {
                string message = "Google sign-in was cancelled or failed. Please try again.";

                // Extra detail only while the site runs in debug mode.
                if (HttpContext.IsDebuggingEnabled)
                {
                    bool cookieFound = Request.Cookies[".AspNet.ExternalCookie"] != null;
                    message += " [Details: " + (Startup.LastOwinProblem ?? "no OWIN error was logged")
                               + " | external cookie " + (cookieFound ? "found" : "missing") + "]";
                }

                return Fail(message);
            }

            string email = loginInfo.Email ?? GetClaim(loginInfo.ExternalIdentity, ClaimTypes.Email);

            string name = GetClaim(loginInfo.ExternalIdentity, ClaimTypes.Name)
                          ?? GetClaim(loginInfo.ExternalIdentity, ClaimTypes.GivenName)
                          ?? "Google User";

            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail("Could not get your email from Google. Please try again or use email and password.");
            }

            // Only trust emails that Google has verified.
            if (GetClaim(loginInfo.ExternalIdentity, Startup.VerifiedEmailClaim) != "true")
            {
                return Fail("Google could not confirm that your email is verified, so we cannot sign you in with it.");
            }

            email = email.Trim().ToLower();

            User user;
            try
            {
                user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

                if (user == null)
                {
                    var customerType = _context.UserTypes.FirstOrDefault(t => t.TypeName == "Customer")
                                       ?? _context.UserTypes.FirstOrDefault();

                    if (customerType == null)
                    {
                        return Fail("Sign-up is temporarily unavailable. Please contact support.");
                    }

                    user = new User
                    {
                        FullName = name.Trim(),
                        Email = email,
                        PasswordHash = "",   // Google-only account, no password
                        UserTypeId = customerType.UserTypeId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                return Fail("We could not finish signing you in. Please try again in a moment.");
            }

            // Same session keys as LoginController.
            Session["UserId"] = user.UserId;
            Session["UserName"] = user.FullName;
            Session["UserTypeId"] = user.UserTypeId;

            if (user.UserTypeId == 3)
            {
                var vendor = _context.Vendors.FirstOrDefault(v => v.VendorName.Contains(user.FullName));
                Session["VendorId"] = vendor?.VendorId;
            }
            else
            {
                Session["VendorId"] = null;
            }

            AuthManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            return RedirectToAction("Index", "DashboardRouter", new { area = "AdminArea" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}