using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    [RequireRole(UserRole.Receptionist, UserRole.Manager)]
    public class UserController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // ---------- Registration ----------

        public ActionResult Register()
        {
            ViewBag.IsManager = CurrentUserRole == UserRole.Manager;
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User model)
        {
            ViewBag.IsManager = CurrentUserRole == UserRole.Manager;

            if (CurrentUserRole != UserRole.Manager && model.Role != UserRole.Customer && model.Role != UserRole.Mechanic)
                model.Role = UserRole.Customer;

            if (db.Users.Any(u => u.Email == model.Email))
                ModelState.AddModelError("Email", "A user with this email already exists.");

            bool isStaffRole = model.Role == UserRole.Receptionist || model.Role == UserRole.Manager || model.Role == UserRole.Mechanic || model.Role == UserRole.Driver;
            if (isStaffRole)
            {
                if (!EmailPolicy.IsAllowedStaffDomain(model.Email))
                    ModelState.AddModelError("Email", EmailPolicy.StaffRequirementsText);
            }
            else
            {
                if (!EmailPolicy.IsAllowedDomain(model.Email))
                    ModelState.AddModelError("Email", EmailPolicy.RequirementsText);
            }

            if (!PasswordPolicy.IsStrong(model.Password))
                ModelState.AddModelError("Password", PasswordPolicy.RequirementsText);

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

            if (model.Role == UserRole.Customer)
                return RedirectToAction("Create", "Vehicle", new { ownerId = model.Id });

            return RedirectToAction("Register");
        }

        // ---------- Customer list + limited edit (Receptionist), full edit (Manager) ----------

        public ActionResult Customers()
        {
            var customers = db.Users.Where(u => u.Role == UserRole.Customer)
                .OrderBy(u => u.FullName).ToList();
            return View(customers);
        }

        public ActionResult EditCustomer(int id)
        {
            var customer = db.Users.FirstOrDefault(u => u.Id == id && u.Role == UserRole.Customer);
            if (customer == null) return HttpNotFound();

            ViewBag.IsManager = CurrentUserRole == UserRole.Manager;
            ViewBag.PendingRequests = db.UserChangeRequests
                .Where(r => r.TargetUserId == id && r.Status == ChangeRequestStatus.Pending)
                .ToList();

            // Show every vehicle this customer has ever brought in - a
            // customer can register as many vehicles as they own.
            var vehicles = db.Vehicles.Where(v => v.OwnerId == id).ToList();
            ViewBag.Vehicles = vehicles;
            ViewBag.HasHistory = vehicles.Any() || db.JobCards.Any(j => j.CustomerId == id);

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCustomer(int id, string fullName, string email, string phone, string address, string city, string postalCode, string driversLicenseNumber)
        {
            var customer = db.Users.FirstOrDefault(u => u.Id == id && u.Role == UserRole.Customer);
            if (customer == null) return HttpNotFound();

            bool isManager = CurrentUserRole == UserRole.Manager;
            ViewBag.IsManager = isManager;
            ViewBag.PendingRequests = db.UserChangeRequests
                .Where(r => r.TargetUserId == id && r.Status == ChangeRequestStatus.Pending)
                .ToList();
            var vehicles = db.Vehicles.Where(v => v.OwnerId == id).ToList();
            ViewBag.Vehicles = vehicles;
            ViewBag.HasHistory = vehicles.Any() || db.JobCards.Any(j => j.CustomerId == id);

            if (!string.IsNullOrWhiteSpace(email) && !EmailPolicy.IsAllowedDomain(email))
            {
                TempData["Error"] = EmailPolicy.RequirementsText;
                return RedirectToAction("EditCustomer", new { id = id });
            }

            if (!FieldValidation.IsValidPhone(phone))
            {
                TempData["Error"] = FieldValidation.PhoneRequirementsText;
                return RedirectToAction("EditCustomer", new { id = id });
            }

            if (!FieldValidation.IsValidPostalCode(postalCode))
            {
                TempData["Error"] = FieldValidation.PostalCodeRequirementsText;
                return RedirectToAction("EditCustomer", new { id = id });
            }

            if (!string.IsNullOrWhiteSpace(fullName) && !FieldValidation.IsValidName(fullName))
            {
                TempData["Error"] = FieldValidation.NameRequirementsText;
                return RedirectToAction("EditCustomer", new { id = id });
            }

            customer.Phone = phone;
            customer.Address = address;
            customer.City = city;
            customer.PostalCode = postalCode;
            customer.DriversLicenseNumber = driversLicenseNumber;

            var messages = new System.Collections.Generic.List<string>();

            if (isManager)
            {
                if (!string.IsNullOrWhiteSpace(fullName)) customer.FullName = fullName;
                if (!string.IsNullOrWhiteSpace(email)) customer.Email = email;
                messages.Add("Details updated.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(fullName) && fullName != customer.FullName)
                {
                    db.UserChangeRequests.Add(new UserChangeRequest
                    {
                        TargetUserId = customer.Id,
                        FieldName = "FullName",
                        OldValue = customer.FullName,
                        NewValue = fullName,
                        RequestedByUserId = CurrentUserId.Value
                    });
                    messages.Add("Name change sent to a Manager for approval.");
                }
                if (!string.IsNullOrWhiteSpace(email) && email != customer.Email)
                {
                    db.UserChangeRequests.Add(new UserChangeRequest
                    {
                        TargetUserId = customer.Id,
                        FieldName = "Email",
                        OldValue = customer.Email,
                        NewValue = email,
                        RequestedByUserId = CurrentUserId.Value
                    });
                    messages.Add("Email change sent to a Manager for approval.");
                }
                messages.Add("Contact details updated.");
            }

            db.SaveChanges();

            TempData["Success"] = string.Join(" ", messages);
            return RedirectToAction("EditCustomer", new { id = id });
        }

        // POST: /User/ResetCustomerPassword/5 - Manager sets it directly.
        // A Receptionist no longer resets a customer's password on the spot -
        // they must request it via RequestCustomerPasswordReset below, and a
        // Manager reviews and fulfills the request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult ResetCustomerPassword(int id, string newPassword)
        {
            var customer = db.Users.FirstOrDefault(u => u.Id == id && u.Role == UserRole.Customer);
            if (customer == null) return HttpNotFound();

            if (!PasswordPolicy.IsStrong(newPassword))
            {
                TempData["Error"] = "Password doesn't meet the requirements: " + PasswordPolicy.RequirementsText;
                return RedirectToAction("EditCustomer", new { id = id });
            }

            string hash, salt;
            PasswordHelper.CreateHash(newPassword, out hash, out salt);
            customer.PasswordHash = hash;
            customer.PasswordSalt = salt;
            db.SaveChanges();

            TempData["Success"] = "Password reset for " + customer.FullName + ". Give them the new password directly.";
            return RedirectToAction("EditCustomer", new { id = id });
        }

        // POST: /User/RequestCustomerPasswordReset/5 - Receptionist asks a
        // Manager to reset this customer's password. No new password is
        // typed here - it just flags the request; the Manager sets the
        // actual password when they fulfill it, so a plain-text password
        // never sits in the change-request table waiting for approval.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestCustomerPasswordReset(int id, string reason)
        {
            var customer = db.Users.FirstOrDefault(u => u.Id == id && u.Role == UserRole.Customer);
            if (customer == null) return HttpNotFound();

            bool alreadyPending = db.UserChangeRequests.Any(r =>
                r.TargetUserId == id && r.FieldName == "PasswordReset" && r.Status == ChangeRequestStatus.Pending);

            if (alreadyPending)
            {
                TempData["Error"] = "There's already a pending password reset request for this customer.";
                return RedirectToAction("EditCustomer", new { id = id });
            }

            db.UserChangeRequests.Add(new UserChangeRequest
            {
                TargetUserId = customer.Id,
                FieldName = "PasswordReset",
                NewValue = string.IsNullOrWhiteSpace(reason) ? "(no reason given)" : reason,
                RequestedByUserId = CurrentUserId.Value
            });
            db.SaveChanges();

            TempData["Success"] = "Password reset request sent to a Manager for " + customer.FullName + ".";
            return RedirectToAction("EditCustomer", new { id = id });
        }

        // POST: /User/FulfillPasswordResetRequest/5 - Manager reviews the
        // request and sets the actual new password in one step, approving
        // the request at the same time.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult FulfillPasswordResetRequest(int requestId, string newPassword)
        {
            var request = db.UserChangeRequests.Find(requestId);
            if (request == null || request.FieldName != "PasswordReset") return HttpNotFound();

            var customer = db.Users.Find(request.TargetUserId);
            if (customer == null) return HttpNotFound();

            if (!PasswordPolicy.IsStrong(newPassword))
            {
                TempData["Error"] = "Password doesn't meet the requirements: " + PasswordPolicy.RequirementsText;
                return RedirectToAction("ChangeRequests");
            }

            string hash, salt;
            PasswordHelper.CreateHash(newPassword, out hash, out salt);
            customer.PasswordHash = hash;
            customer.PasswordSalt = salt;

            request.Status = ChangeRequestStatus.Approved;
            request.ReviewedByUserId = CurrentUserId;
            request.ReviewedAtUtc = DateTime.UtcNow;
            db.SaveChanges();

            TempData["Success"] = "Password reset for " + customer.FullName + ". Give them the new password directly.";
            return RedirectToAction("ChangeRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCustomer(int id)
        {
            var customer = db.Users.FirstOrDefault(u => u.Id == id && u.Role == UserRole.Customer);
            if (customer == null) return HttpNotFound();

            bool hasHistory = db.Vehicles.Any(v => v.OwnerId == id) || db.JobCards.Any(j => j.CustomerId == id);

            if (hasHistory)
            {
                customer.IsActive = false;
                db.SaveChanges();
                TempData["Success"] = customer.FullName + " has vehicles/job history on file, so they can't be fully deleted - deactivated instead. They can no longer log in.";
            }
            else
            {
                db.Users.Remove(customer);
                db.SaveChanges();
                TempData["Success"] = customer.FullName + " was permanently removed.";
            }

            return RedirectToAction("Customers");
        }

        // ---------- Manager: full account management for staff ----------

        [RequireRole(UserRole.Manager)]
        public ActionResult ManageAccounts()
        {
            var staff = db.Users
                .Where(u => u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver)
                .OrderBy(u => u.Role).ThenBy(u => u.FullName)
                .ToList();
            return View(staff);
        }

        [RequireRole(UserRole.Manager)]
        public ActionResult EditStaff(int id)
        {
            var staff = db.Users.FirstOrDefault(u => u.Id == id && (u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver));
            if (staff == null) return HttpNotFound();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult EditStaff(int id, string fullName, string email, string phone, UserRole role, HttpPostedFileBase photoFile)
        {
            var staff = db.Users.FirstOrDefault(u => u.Id == id && (u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver));
            if (staff == null) return HttpNotFound();

            if (!EmailPolicy.IsAllowedStaffDomain(email))
            {
                TempData["Error"] = EmailPolicy.StaffRequirementsText;
                return RedirectToAction("EditStaff", new { id = id });
            }

            if (db.Users.Any(u => u.Email == email && u.Id != id))
            {
                TempData["Error"] = "Another account already uses that email.";
                return RedirectToAction("EditStaff", new { id = id });
            }

            if (!FieldValidation.IsValidPhone(phone))
            {
                TempData["Error"] = FieldValidation.PhoneRequirementsText;
                return RedirectToAction("EditStaff", new { id = id });
            }

            if (!FieldValidation.IsValidName(fullName))
            {
                TempData["Error"] = FieldValidation.NameRequirementsText;
                return RedirectToAction("EditStaff", new { id = id });
            }

            staff.FullName = fullName;
            staff.Email = email;
            staff.Phone = phone;
            if (role == UserRole.Mechanic || role == UserRole.Receptionist || role == UserRole.Driver)
                staff.Role = role;

            if (photoFile != null && photoFile.ContentLength > 0)
                staff.ProfilePhotoUrl = SaveStaffPhoto(photoFile);

            db.SaveChanges();

            TempData["Success"] = staff.FullName + "'s details were updated.";
            return RedirectToAction("ManageAccounts");
        }

 // Saves an uploaded staff photo under ~/Content/images/staff/ with a
        // unique filename, and returns the relative URL to store.
        // (Azure Blob Storage migration paused for now - reverting to local
        // disk so the app compiles and works without needing Azure set up.)
        private string SaveStaffPhoto(HttpPostedFileBase file)
        {
            var folder = Server.MapPath("~/Content/images/staff");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            file.SaveAs(Path.Combine(folder, fileName));

            return "/Content/images/staff/" + fileName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult ResetStaffPassword(int id, string newPassword)
        {
            var staff = db.Users.FirstOrDefault(u => u.Id == id && (u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver));
            if (staff == null) return HttpNotFound();

            if (!PasswordPolicy.IsStrong(newPassword))
            {
                TempData["Error"] = "Password doesn't meet the requirements: " + PasswordPolicy.RequirementsText;
                return RedirectToAction("EditStaff", new { id = id });
            }

            string hash, salt;
            PasswordHelper.CreateHash(newPassword, out hash, out salt);
            staff.PasswordHash = hash;
            staff.PasswordSalt = salt;
            db.SaveChanges();

            TempData["Success"] = "Password reset for " + staff.FullName + ". Give them the new password directly.";
            return RedirectToAction("EditStaff", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult Activate(int id)
        {
            var staff = db.Users.FirstOrDefault(u => u.Id == id && (u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver));
            if (staff == null) return HttpNotFound();

            staff.IsActive = true;
            db.SaveChanges();

            TempData["Success"] = staff.FullName + " reactivated - they can log in again.";
            return RedirectToAction("ManageAccounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult Deactivate(int id)
        {
            var staff = db.Users.FirstOrDefault(u => u.Id == id && (u.Role == UserRole.Mechanic || u.Role == UserRole.Receptionist || u.Role == UserRole.Driver));
            if (staff == null) return HttpNotFound();

            if (staff.Id == CurrentUserId)
            {
                TempData["Error"] = "You can't deactivate your own account.";
                return RedirectToAction("ManageAccounts");
            }

            staff.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = staff.FullName + " deactivated - they can no longer log in.";
            return RedirectToAction("ManageAccounts");
        }

        // ---------- Manager: review pending change requests ----------

        [RequireRole(UserRole.Manager)]
        public ActionResult ChangeRequests()
        {
            var requests = db.UserChangeRequests
                .Include(r => r.TargetUser)
                .Include(r => r.RequestedBy)
                .Where(r => r.Status == ChangeRequestStatus.Pending)
                .OrderBy(r => r.RequestedAtUtc)
                .ToList();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult ApproveChangeRequest(int id)
        {
            var request = db.UserChangeRequests.Find(id);
            if (request == null) return HttpNotFound();

            var target = db.Users.Find(request.TargetUserId);
            if (target != null)
            {
                if (request.FieldName == "FullName") target.FullName = request.NewValue;
                else if (request.FieldName == "Email") target.Email = request.NewValue;
            }

            request.Status = ChangeRequestStatus.Approved;
            request.ReviewedByUserId = CurrentUserId;
            request.ReviewedAtUtc = DateTime.UtcNow;
            db.SaveChanges();

            TempData["Success"] = "Change approved and applied.";
            return RedirectToAction("ChangeRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult RejectChangeRequest(int id)
        {
            var request = db.UserChangeRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = ChangeRequestStatus.Rejected;
            request.ReviewedByUserId = CurrentUserId;
            request.ReviewedAtUtc = DateTime.UtcNow;
            db.SaveChanges();

            TempData["Success"] = "Change request rejected.";
            return RedirectToAction("ChangeRequests");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
