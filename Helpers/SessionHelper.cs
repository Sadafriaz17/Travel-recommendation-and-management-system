using System.Web;

namespace TourwebsiteFYP.Helpers
{
    /// <summary>
    /// Reads the logged-in user from the session.
    ///
    /// After a successful login the app stores these session keys
    /// (see LoginController):
    ///     UserId, UserName, UserTypeId  and VendorId (vendors only).
    ///
    /// UserTypeIds used in this project:
    ///     1 = Admin, 2 = Customer, 3 = Vendor, 4 = Manager, 5 = Customer.
    /// </summary>
    public static class SessionHelper
    {
        /// <summary>Id of the logged-in user, or 0 when nobody is logged in.</summary>
        public static int GetUserId()
        {
            return ToInt(HttpContext.Current.Session["UserId"]);
        }

        /// <summary>Full name of the logged-in user, or an empty string.</summary>
        public static string GetUserName()
        {
            object name = HttpContext.Current.Session["UserName"];
            return name == null ? string.Empty : name.ToString();
        }

        /// <summary>UserTypeId of the logged-in user, or 0 when nobody is logged in.</summary>
        public static int GetUserTypeId()
        {
            return ToInt(HttpContext.Current.Session["UserTypeId"]);
        }

        /// <summary>
        /// VendorId of the logged-in vendor, or null when the user is not a
        /// vendor / has no vendor record yet.
        /// </summary>
        public static int? GetVendorId()
        {
            int vendorId = ToInt(HttpContext.Current.Session["VendorId"]);
            return vendorId > 0 ? vendorId : (int?)null;
        }

        /// <summary>True when a user is logged in.</summary>
        public static bool IsLoggedIn()
        {
            return HttpContext.Current.Session["UserId"] != null;
        }

        /// <summary>Converts a session value to an int (0 when it is missing or not a number).</summary>
        private static int ToInt(object value)
        {
            int number;
            if (value == null) return 0;
            int.TryParse(value.ToString(), out number);
            return number;
        }
    }
}
