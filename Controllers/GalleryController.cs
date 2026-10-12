using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class GalleryController : Controller
    {
        private Demo_DevDBEntities _db = new Demo_DevDBEntities();
        // GET: Blog
        public ActionResult Index()
        {
            return View();
        }
    }
}