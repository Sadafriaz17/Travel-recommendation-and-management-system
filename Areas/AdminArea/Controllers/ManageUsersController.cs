using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;

namespace TourwebsiteFYP.Areas.AdminArea.Controllers
{
    /// <summary>
    /// User management CRUD. Full access is Admin-only (TypeId 1).
    /// The Delete action carries an extra RoleAuthFilter so that even if
    /// someone navigates directly to the URL they are bounced away.
    /// </summary>
    [SessionAuthFilter]
    [RoleAuthFilter(1)]
    public class ManageUsersController : Controller
    {
        private Demo_DevDBEntities _context = new Demo_DevDBEntities();

        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Users";
            ViewBag.ActivePage = "Users";
            var users = _context.Users.Include(u => u.UserType).ToList();
            return View(users);
        }

        public ActionResult Create()
        {
            ViewBag.ActiveMenu   = "Users";
            ViewBag.ActivePage   = "Users";
            ViewBag.UserTypeList = _context.UserTypes.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Create(User model)
        {
            model.CreatedAt = DateTime.Now;
            _context.Users.Add(model);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User created successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveMenu   = "Users";
            ViewBag.ActivePage   = "Users";
            var user = _context.Users.Find(id);
            if (user == null) return HttpNotFound();
            ViewBag.UserTypeList = _context.UserTypes.ToList();
            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(User model)
        {
            var existingUser = _context.Users.Find(model.UserId);
            if (existingUser != null)
            {
                existingUser.FullName    = model.FullName;
                existingUser.Email       = model.Email;
                existingUser.PasswordHash = model.PasswordHash;
                existingUser.Phone       = model.Phone;
                existingUser.UserTypeId  = model.UserTypeId;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction("Index");
            }
            return HttpNotFound();
        }

        /// <summary>
        /// Delete is additionally protected by RoleAuthFilter(1) at the
        /// controller level, so Managers and others cannot delete users.
        /// </summary>
        public ActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found!";
            }
            return RedirectToAction("Index");
        }
    }
}