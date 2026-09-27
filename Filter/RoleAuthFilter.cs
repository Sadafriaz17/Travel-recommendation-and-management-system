using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TourwebsiteFYP.Filter
{
    /// <summary>
    /// Action filter that gates access to a controller or action by UserTypeId.
    /// Applied in addition to SessionAuthFilter (which only checks login status).
    ///
    /// Usage:
    ///   [RoleAuthFilter(1)]              // Admin only
    ///   [RoleAuthFilter(1, 4)]           // Admin OR Manager
    ///   [RoleAuthFilter(2, 5)]           // Customer (both TypeIds)
    ///
    /// On an unauthorized logged-in user the filter redirects to the role
    /// dashboard router so they land on their own dashboard instead of a 403.
    /// </summary>
    public class RoleAuthFilter : ActionFilterAttribute
    {
        private readonly int[] _allowedTypeIds;

        public RoleAuthFilter(params int[] allowedTypeIds)
        {
            _allowedTypeIds = allowedTypeIds ?? new int[0];
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;

            // Not logged in  handled by SessionAuthFilter; redirect to login.
            if (session["UserId"] == null)
            {
                filterContext.Result = new RedirectResult("/Login/Index");
                return;
            }

            int typeId = 0;
            if (session["UserTypeId"] != null)
                int.TryParse(session["UserTypeId"].ToString(), out typeId);

            // If no restriction specified, allow any logged-in user.
            if (_allowedTypeIds.Length == 0)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            if (!_allowedTypeIds.Contains(typeId))
            {
                // Redirect the unauthorised user to their own dashboard.
                filterContext.Result = new RedirectResult("/AdminArea/DashboardRouter/Index");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
