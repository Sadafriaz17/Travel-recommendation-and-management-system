using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;   // adjust namespace to your EF models

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    [SessionAuthFilter]
    public class ManageUserTypeController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        // GET: AdminArea/ManageUserType
        public ActionResult Index()
        {
            var userTypes = _context.UserTypes.ToList();
            return View(userTypes);
        }

        // GET: AdminArea/ManageUserType/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminArea/ManageUserType/Create
        [HttpPost]
       
        public ActionResult Create(UserType userType)
        {
            if (ModelState.IsValid)
            {
                _context.UserTypes.Add(userType);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userType);
        }

        // GET: AdminArea/ManageUserType/Edit/5
        public ActionResult Edit(int id)
        {
            var userType = _context.UserTypes.Find(id);
            if (userType == null) return HttpNotFound();
            return View(userType);
        }

        // POST: AdminArea/ManageUserType/Edit/5
        [HttpPost]
       
        public ActionResult Edit(UserType Model)
        {
            _context.UserTypes.AddOrUpdate(Model);
            _context.SaveChanges();
            return RedirectToAction("index");
        }

        // GET: AdminArea/ManageUserType/Delete/5
        public ActionResult Delete(int id)
        {
            var data = _context.UserTypes.FirstOrDefault(x => x.UserTypeId == id);
            _context.UserTypes.Remove(data);
            _context.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";
            return RedirectToAction("index");
        }
    }
}
