using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;

namespace TourwebsiteFYP.Controllers
{
    public class VendorMarketPlaceController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();
       
        public ActionResult Index()
        {
            return View();
        }
    }
}