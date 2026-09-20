using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    /// <summary>
    /// Handles Google OAuth sign-in / sign-up for the user-facing site.
    /// Works with the existing custom session auth (Session["UserId"] etc.)
    /// and does NOT use ASP.NET Identity as the primary auth store.
    /// </summary>
    public class GoogleAuthController : Controller
    {
        private readonly Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // ------------------------------------------------------------------
        // Helper: safely read a single claim value from a ClaimsIdentity.
        // Uses LINQ instead of the .FindFirstValue() extension method so
        // we don't need an extra using / assembly reference.
        // ------------------------------------------------------------------
        private static string GetClaim(ClaimsIdentity identity, string claimType)
        {
            return identity?.Claims
                            .FirstOrDefault(c => c.Type == claimType)
                           ?.Value;
        }

        // ------------------------------------------------------------------
        // STEP 1: Issue the OAuth challenge → redirects browser to Google
        // ------------------------------------------------------------------
        [AllowAnonymous]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUri = Url.Action("ExternalLoginCallback", "GoogleAuth",
                                         new { ReturnUrl = returnUrl },
                                         protocol: Request.Url.Scheme);

            HttpContext.GetOwinContext()
                       .Authentication
                       .Challenge(new AuthenticationProperties { RedirectUri = redirectUri },
                                  provider);

            // HttpUnauthorizedResult (401) triggers OWIN to intercept and
            // redirect the browser to the Google consent page.
            return new HttpUnauthorizedResult();
        }

        // ------------------------------------------------------------------
        // STEP 2: Google redirects back here after the user consents
        // ------------------------------------------------------------------
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            // Retrieve the external login identity Google put into the
            // external cookie.
            var loginInfo = await HttpContext.GetOwinContext()
                                             .Authentication
                                             .GetExternalLoginInfoAsync();

            if (loginInfo == null)
            {
                // OWIN could not read the external cookie — user may have
                // cancelled, or the external cookie expired.
                TempData["LoginError"] = "Google sign-in was cancelled or failed. Please try again.";
                return RedirectToAction("Index", "Login");
            }

            // ----------------------------------------------------------
            // Clarification #3: Robust claim retrieval — loginInfo.Email
            // can be null depending on the OWIN Google provider version.
            // Fall back to reading the email claim explicitly.
            // ----------------------------------------------------------
            string email = loginInfo.Email
                           ?? GetClaim(loginInfo.ExternalIdentity, ClaimTypes.Email);

            string name = GetClaim(loginInfo.ExternalIdentity, ClaimTypes.Name)
                          ?? GetClaim(loginInfo.ExternalIdentity, ClaimTypes.GivenName)
                          ?? "Google User";

            if (string.IsNullOrWhiteSpace(email))
            {
                // Cannot proceed without an email — show a friendly message.
                TempData["LoginError"] = "Could not retrieve your email from Google — " +
                                         "please try again or use the email/password login.";
                return RedirectToAction("Index", "Login");
            }

            // Normalise the email the same way the rest of the app does.
            email = email.Trim().ToLower();

            // ----------------------------------------------------------
            // Find existing user by email, OR create a new one.
            //
            // Clarification #4: Account linking — if a row already exists
            // with a PasswordHash (password-signup account), we deliberately
            // link the Google login to that account by email and sign them
            // in. No duplicate row is created.
            // ----------------------------------------------------------
            var user = _context.Users
                               .FirstOrDefault(u => u.Email.ToLower() == email);

            if (user == null)
            {
                // New user — auto-register as Customer.
                // Look up the Customer UserType dynamically (same pattern as SignupController).
                var customerType = _context.UserTypes
                                           .FirstOrDefault(t => t.TypeName == "Customer")
                                   ?? _context.UserTypes.FirstOrDefault();

                if (customerType == null)
                {
                    TempData["LoginError"] = "Registration is temporarily unavailable — " +
                                             "no user type is configured. Please contact support.";
                    return RedirectToAction("Index", "Login");
                }

                user = new User
                {
                    FullName     = name.Trim(),
                    Email        = email,
                    PasswordHash = null,          // Google-only account — no password
                    UserTypeId   = customerType.UserTypeId,
                    CreatedAt    = DateTime.Now
                };

                _context.Users.Add(user);
                _context.SaveChanges();
            }

            // ----------------------------------------------------------
            // Establish the session exactly as LoginController.Index(POST)
            // does — same three session keys, same values.
            // ----------------------------------------------------------
            Session["UserId"]     = user.UserId;
            Session["UserName"]   = user.FullName;
            Session["UserTypeId"] = user.UserTypeId;

            // Sign out of the transient external cookie — we're now using
            // the application's own session mechanism.
            HttpContext.GetOwinContext().Authentication
                       .SignOut(DefaultAuthenticationTypes.ExternalCookie);

            // ----------------------------------------------------------
            // Clarification #6: Post-login redirect matches LoginController.
            // LoginController always redirects to Home/Index regardless of
            // role — replicate exactly.  If LoginController ever gains
            // role-based branching, update both places in sync.
            // ----------------------------------------------------------
            return RedirectToAction("Index", "Home");
        }
    }
}
