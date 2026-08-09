using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageGalleryController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManageGallery
        public ActionResult Index()
        {
            var gallery = _context.Galleries.ToList();
            return View(gallery);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
      
        public ActionResult Create(Gallery Model)
        {
            
                _context.Galleries.Add(Model);
                _context.SaveChanges();
                return RedirectToAction("Index");
           
        }

        public ActionResult Edit(int id)
        {
           
            var gallery = _context.Galleries.Find(id);
            if (gallery == null) return HttpNotFound();
            return View(gallery);
        }

        [HttpPost]
      
        public ActionResult Edit(Gallery Model, HttpPostedFileBase img)
        {
            if (img != null && img.ContentLength > 0)
            {
                //name
                string fileName = Path.GetFileName(img.FileName);
                //url
                string filePath = Server.MapPath("~/uploads/gallery/");
                //filepath exists
                if (!Directory.Exists(filePath))
                {

                    Directory.CreateDirectory(filePath);
                }
                //create full path
                string fullPath = Path.Combine(filePath, fileName);
                //image save 
                img.SaveAs(fullPath);
                //image save in db
                Model.ImageUrl = "/uploads/gallery/" + fileName;
            }


            _context.Galleries.AddOrUpdate(Model);
            _context.SaveChanges();
            return RedirectToAction("index");
        }
             

        public ActionResult Delete(int id)
        {

            var gallery = _context.Galleries.Find(id);
            _context.Galleries.Remove(gallery);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        
    }
}
