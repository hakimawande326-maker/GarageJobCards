using System;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    public class AccountController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // GET: /Account/Login
        public ActionResult Login(string returnUrl, string role)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.SelectedRole = string.IsNullOrEmpty(role) ? "Receptionist" : role;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string returnUrl, string role)
        {
            ViewBag.SelectedRole = string.IsNullOrEmpty(role) ? "Receptionist" : role;

            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                ViewBag.Error = "Incorrect email or password.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            UserRole selectedRole;
            if (Enum.TryParse(role, out selectedRole) && user.Role != selectedRole)
            {
                ViewBag.Error = "That account is registered as " + user.Role + ", not " + role + " - switch tabs above and try again.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            Session["UserId"] = user.Id;
            Session["UserName"] = user.FullName;
            Session["UserRole"] = user.Role.ToString();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Role-based landing page
            switch (user.Role)
            {
                case UserRole.Mechanic:
                    return RedirectToAction("MyJobs", "JobCard");
                case UserRole.Customer:
                    return RedirectToAction("MyBookings", "JobCard");
                case UserRole.Manager:
                    return RedirectToAction("Index", "Analytics");
                default: // Receptionist
                    return RedirectToAction("Index", "JobCard");
            }
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public ActionResult AccessDenied()
        {
            return View();
        }

        // ---------- Forgot / reset password ----------

        // GET: /Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            // Always show the same confirmation whether or not the email exists -
            // this stops someone from using this form to discover which emails
            // are registered.
            if (user != null)
            {
                user.PasswordResetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                user.PasswordResetTokenExpiryUtc = DateTime.UtcNow.AddHours(1);
                db.SaveChanges();

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token = user.PasswordResetToken }, Request.Url.Scheme);

                try
                {
                    EmailHelper.SendPasswordResetEmail(user.Email, resetLink);
                }
                catch (Exception ex)
                {
                    // Don't let a broken SMTP config crash the request or leak
                    // whether the email existed - log and fall through to the
                    // same confirmation screen either way.
                    System.Diagnostics.Trace.TraceError("Password reset email failed: " + ex.Message);
                }
            }

            return View("ForgotPasswordConfirmation");
        }

        // GET: /Account/ResetPassword?token=...
        public ActionResult ResetPassword(string token)
        {
            var user = db.Users.FirstOrDefault(u => u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpiryUtc == null || user.PasswordResetTokenExpiryUtc < DateTime.UtcNow)
            {
                return View("ResetPasswordInvalid");
            }

            ViewBag.Token = token;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            var user = db.Users.FirstOrDefault(u => u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpiryUtc == null || user.PasswordResetTokenExpiryUtc < DateTime.UtcNow)
            {
                return View("ResetPasswordInvalid");
            }

            ViewBag.Token = token;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords don't match.";
                return View();
            }

            string hash, salt;
            PasswordHelper.CreateHash(newPassword, out hash, out salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiryUtc = null;
            db.SaveChanges();

            TempData["Success"] = "Password updated - log in with your new password.";
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
