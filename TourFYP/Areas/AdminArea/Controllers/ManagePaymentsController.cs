using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManagePaymentsController : Controller
    {
        private readonly Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManagePayments
        public ActionResult Index()
        {
            var payments = _context.Payments.ToList();
            return View(payments);
        }

       
        public ActionResult Details(int id)
        {
            var payment = _context.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }





    }
}
