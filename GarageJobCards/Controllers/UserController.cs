using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
 [RequireRole(UserRole.Receptionist, UserRole.Manager)]
 public class UserController : BaseController
 {
 private readonly GarageContext db = new GarageContext();

 // GET: /User/Register
 public ActionResult Register()
 {
 // Receptionists can only register customers; managers can register any role.
 ViewBag.CanChooseRole = CurrentUserRole == UserRole.Manager;
 return View(new User());
 }

 // POST: /User/Register
 [HttpPost]
 [ValidateAntiForgeryToken]
 public ActionResult Register(User model)
 {
 ViewBag.CanChooseRole = CurrentUserRole == UserRole.Manager;

 // Receptionists are locked to registering Customers regardless of what's posted.
 if (CurrentUserRole != UserRole.Manager)
 model.Role = UserRole.Customer;

 if (db.Users.Any(u => u.Email == model.Email))
 ModelState.AddModelError("Email", "A user with this email already exists.");

 if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
 ModelState.AddModelError("Password", "Password must be at least 6 characters.");

 ModelState.Remove("PasswordHash");
 ModelState.Remove("PasswordSalt");

 if (!ModelState.IsValid)
 return View(model);

 string hash, salt;
 PasswordHelper.CreateHash(model.Password, out hash, out salt);
 model.PasswordHash = hash;
 model.PasswordSalt = salt;

 db.Users.Add(model);
 db.SaveChanges();

 TempData["Success"] = model.FullName + " was registered as a " + model.Role + ".";

 // If a receptionist just registered a customer, jump straight into
 // registering that customer's vehicle.
 if (model.Role == UserRole.Customer)
 return RedirectToAction("Create", "Vehicle", new { ownerId = model.Id });

 return RedirectToAction("Register");
 }

 protected override void Dispose(bool disposing)
 {
 if (disposing) db.Dispose();
 base.Dispose(disposing);
 }
 }
}
