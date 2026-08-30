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

        // How long an OTP stays valid after it's sent.
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromSeconds(110); // 1:50

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

            if (!user.IsActive)
            {
                ViewBag.Error = "This account has been deactivated. Contact a manager for help.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            user.LastLoginUtc = DateTime.UtcNow;
            db.SaveChanges();

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

        // ---------- Forgot password via SMS OTP - works for every role ----------

        // GET: /Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email, string phone)
        {
            // Both the email AND the phone must match the SAME account -
            // this is the two-factor check that proves it's really them
            // before we send a code.
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.Phone == phone);

            if (user == null)
            {
                ViewBag.Error = "We couldn't find an account with that email and phone number combination.";
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiryUtc = DateTime.UtcNow.Add(OtpLifetime);
            db.SaveChanges();

            try
            {
                SmsHelper.SendSms(user.Phone, "Phila's Auto: your password reset code is " + otp + ". It expires in 1 minute 50 seconds.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("OTP SMS failed: " + ex.Message);
                // TEMPORARY: show the real error so we can diagnose the SMS
                // problem. Remove this ViewBag.DebugError line once OTPs are
                // confirmed arriving reliably.
                ViewBag.Error = "Couldn't send the SMS right now - please try again shortly.";
                ViewBag.DebugError = ex.ToString();
                return View();
            }

            TempData["DebugOtp"] = otp; // TEMPORARY - remove once SMS delivery is confirmed working
            return RedirectToAction("VerifyOtp", new { email = email });
        }

        // GET: /Account/VerifyOtp?email=...
        public ActionResult VerifyOtp(string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || user.OtpExpiryUtc == null)
                return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;
            // Tell the browser exactly when the code expires (as epoch millis)
            // so the countdown timer matches the server precisely instead of
            // guessing 110 seconds from whenever the page happens to load.
            ViewBag.ExpiryEpochMillis = ToEpochMillis(user.OtpExpiryUtc.Value);
            ViewBag.DebugOtp = TempData["DebugOtp"]; // TEMPORARY - remove once SMS delivery is confirmed working
            return View();
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(string email, string otpCode, string newPassword, string confirmPassword)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || string.IsNullOrEmpty(user.OtpCode) || user.OtpExpiryUtc == null)
                return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;
            ViewBag.ExpiryEpochMillis = ToEpochMillis(user.OtpExpiryUtc.Value);

            if (user.OtpExpiryUtc < DateTime.UtcNow)
            {
                ViewBag.Error = "That code has expired. Request a new one.";
                return View("ForgotPasswordExpired");
            }

            if (otpCode != user.OtpCode)
            {
                ViewBag.Error = "That code is incorrect.";
                return View();
            }

            if (!PasswordPolicy.IsStrong(newPassword))
            {
                ViewBag.Error = "Password doesn't meet the requirements: " + PasswordPolicy.RequirementsText;
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
            user.OtpCode = null;
            user.OtpExpiryUtc = null;
            db.SaveChanges();

            TempData["Success"] = "Password updated - log in with your new password.";
            return RedirectToAction("Login");
        }

        private static long ToEpochMillis(DateTime utcDateTime)
        {
            return (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}