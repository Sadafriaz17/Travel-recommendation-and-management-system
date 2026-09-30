using System.IO;
using System.Web;

namespace TourwebsiteFYP.Helpers
{
    /// <summary>
    /// Saves uploaded images into the site's /uploads folder.
    ///
    /// Every admin screen that uploads a picture used to repeat the same
    /// "get file name / create folder / SaveAs / build URL" block, so it
    /// lives here once instead.
    /// </summary>
    public static class UploadHelper
    {
        /// <summary>
        /// Saves an uploaded file in ~/uploads/{subFolder} and returns the public
        /// URL, for example "/uploads/Blogs/photo.jpg".
        /// Pass an empty subFolder to save straight into /uploads.
        /// Returns null when no file was uploaded (so callers keep the old image).
        /// </summary>
        public static string SaveImage(HttpPostedFileBase file, string subFolder)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            string urlFolder = "/uploads/";
            if (!string.IsNullOrWhiteSpace(subFolder))
                urlFolder += subFolder.Trim('/') + "/";

            // "~" turns the web path into a real folder path on the server.
            string physicalFolder = HttpContext.Current.Server.MapPath("~" + urlFolder);
            Directory.CreateDirectory(physicalFolder);   // does nothing when it already exists

            string fileName = Path.GetFileName(file.FileName);
            file.SaveAs(Path.Combine(physicalFolder, fileName));

            return urlFolder + fileName;
        }
    }
}
