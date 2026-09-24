using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    public class JobCardController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Index()
        {
            var jobs = db.JobCards
                .Include(j => j.Vehicle)
                .Include(j => j.Customer)
                .Include(j => j.AssignedMechanic)
                .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
                .OrderByDescending(j => j.DateBookedUtc)
                .ToList();

            ViewBag.Mechanics = db.Users.Where(u => u.Role == UserRole.Mechanic).OrderBy(u => u.FullName).ToList();

            return View(jobs);
        }

        [RequireRole(UserRole.Manager)]
        public ActionResult AssignMechanic(int id)
        {
            var job = db.JobCards.Include(j => j.Vehicle).Include(j => j.Customer).FirstOrDefault(j => j.Id == id);
            if (job == null) return HttpNotFound();

            ViewBag.Mechanics = db.Users.Where(u => u.Role == UserRole.Mechanic).OrderBy(u => u.FullName).ToList();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult AssignMechanic(int id, int mechanicId)
        {
            var job = db.JobCards.Find(id);
            if (job == null) return HttpNotFound();

            job.AssignedMechanicId = mechanicId;
            job.StatusChangedUtc = DateTime.UtcNow;
            db.SaveChanges();

            var mechanic = db.Users.Find(mechanicId);
            if (mechanic != null && !string.IsNullOrWhiteSpace(mechanic.Phone))
            {
                try
                {
                    SmsHelper.SendSms(mechanic.Phone,
                        "Phila's Auto: you've been assigned job " + job.JobNumber +
                        " - " + job.ServiceRequested);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("SMS to mechanic failed: " + ex.Message);
                    TempData["SmsWarning"] = "Mechanic assigned, but the SMS notification couldn't be sent.";
                }
            }

            TempData["Success"] = "Mechanic assigned to " + job.JobNumber + ".";
            return RedirectToAction("Index");
        }

        [RequireRole(UserRole.Mechanic)]
        public ActionResult MyJobs()
        {
            var jobs = db.JobCards
                .Include(j => j.Vehicle)
                .Include(j => j.Customer)
                .Where(j => j.AssignedMechanicId == CurrentUserId
                         && j.Status != JobStatus.Completed
                         && j.Status != JobStatus.Cancelled)
                .OrderBy(j => j.StatusChangedUtc)
                .ToList();

            return View(jobs);
        }

        [HttpPost]
        [RequireRole]
        public JsonResult UpdateStatus(int id, JobStatus status)
        {
            var job = db.JobCards.Find(id);
            if (job == null)
                return Json(new { success = false, message = "Job card not found." });

            if (CurrentUserRole == UserRole.Mechanic && job.AssignedMechanicId != CurrentUserId)
                return Json(new { success = false, message = "This job isn't assigned to you." });

            if (CurrentUserRole == UserRole.Customer)
                return Json(new { success = false, message = "Not permitted." });

            job.Status = status;
            job.StatusChangedUtc = DateTime.UtcNow;
            if (status == JobStatus.Completed)
                job.DateCompletedUtc = DateTime.UtcNow;

            db.SaveChanges();
            return Json(new { success = true, status = job.Status.ToString(), label = JobCard.StatusLabel(job.Status) });
        }

        [HttpPost]
        [RequireRole(UserRole.Mechanic)]
        public JsonResult UpdateEstimate(int id, string hours, string amount)
        {
            var job = db.JobCards.Find(id);
            if (job == null)
                return Json(new { success = false, message = "Job card not found." });

            if (job.AssignedMechanicId != CurrentUserId)
                return Json(new { success = false, message = "This job isn't assigned to you." });

            decimal hoursValue, amountValue;
            bool hasHours = decimal.TryParse(hours, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out hoursValue);
            bool hasAmount = decimal.TryParse(amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amountValue);

            if (!hasHours && !hasAmount)
                return Json(new { success = false, message = "Enter an hours estimate, a quoted amount, or both." });

            if (hasAmount && (amountValue <= 0 || amountValue > 500000m))
                return Json(new { success = false, message = "Quoted amount must be between R0.01 and R500,000." });

            if (hasHours && (hoursValue <= 0 || hoursValue > 500m))
                return Json(new { success = false, message = "Estimated hours must be between 0.1 and 500." });

            if (hasHours)
                job.EstimatedHours = hoursValue;
            if (hasAmount)
                job.EstimateAmount = amountValue;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                hours = job.EstimatedHours,
                amount = job.EstimateAmount,
                estimatedCompletion = job.EstimatedCompletionUtc.HasValue
                    ? job.EstimatedCompletionUtc.Value.ToLocalTime().ToString("dd MMM, HH:mm")
                    : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult SignAndRelease(int id)
        {
            var manager = db.Users.Find(CurrentUserId);
            if (manager == null || string.IsNullOrEmpty(manager.SignatureImageDataUrl))
            {
                TempData["Error"] = "Save your signature first before signing off a job.";
                return RedirectToAction("Index", "Signature");
            }

            var job = db.JobCards.Find(id);
            if (job == null) return HttpNotFound();

            job.Status = JobStatus.ReadyForPickup;
            job.ManagerSignedByUserId = CurrentUserId;
            job.SignedAtUtc = DateTime.UtcNow;
            job.StatusChangedUtc = DateTime.UtcNow;
            db.SaveChanges();

            TempData["Success"] = job.JobNumber + " signed off and marked ready for pickup.";
            return RedirectToAction("Details", new { id = id });
        }

        [RequireRole]
        public ActionResult Details(int id)
        {
            var job = db.JobCards
                .Include(j => j.Vehicle)
                .Include(j => j.Customer)
                .Include(j => j.AssignedMechanic)
                .Include(j => j.CreatedByUser)
                .FirstOrDefault(j => j.Id == id);

            if (job == null) return HttpNotFound();

            bool allowed =
                CurrentUserRole == UserRole.Receptionist ||
                CurrentUserRole == UserRole.Manager ||
                (CurrentUserRole == UserRole.Mechanic && job.AssignedMechanicId == CurrentUserId) ||
                (CurrentUserRole == UserRole.Customer && job.CustomerId == CurrentUserId);

            if (!allowed) return new HttpStatusCodeResult(403);

            return View(job);
        }

        [RequireRole]
        public ActionResult Quotation(int id)
        {
            var job = db.JobCards
                .Include(j => j.Vehicle)
                .Include(j => j.Customer)
                .Include(j => j.AssignedMechanic)
                .Include(j => j.ManagerSignedBy)
                .FirstOrDefault(j => j.Id == id);

            if (job == null) return HttpNotFound();

            bool allowed =
                CurrentUserRole == UserRole.Receptionist ||
                CurrentUserRole == UserRole.Manager ||
                (CurrentUserRole == UserRole.Mechanic && job.AssignedMechanicId == CurrentUserId) ||
                (CurrentUserRole == UserRole.Customer && job.CustomerId == CurrentUserId);

            if (!allowed) return new HttpStatusCodeResult(403);

            if (job.ManagerSignedByUserId == null)
            {
                TempData["Error"] = "This job hasn't been signed off yet - the quotation isn't ready.";
                return RedirectToAction("Details", new { id = id });
            }

            return View(job);
        }

        // ---------- Customer: view their own jobs, approve/decline quotes ----------

        [RequireRole(UserRole.Customer)]
        public ActionResult MyBookings()
        {
            var jobs = db.JobCards
                .Include(j => j.Vehicle)
                .Include(j => j.AssignedMechanic)
                .Where(j => j.CustomerId == CurrentUserId)
                .OrderByDescending(j => j.DateBookedUtc)
                .ToList();

            return View(jobs);
        }

        // POST: /JobCard/Approve/5 - customer accepts the quote and signs.
        // A signature is mandatory - this is the customer's formal consent
        // to proceed with the repair at the quoted price, before any work
        // beyond diagnostics happens.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Customer)]
        public ActionResult Approve(int id, string signatureDataUrl)
        {
            var job = db.JobCards.FirstOrDefault(j => j.Id == id && j.CustomerId == CurrentUserId);
            if (job == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(signatureDataUrl))
            {
                TempData["Error"] = "Please sign before approving the quote.";
                return RedirectToAction("MyBookings");
            }

            if (job.Status == JobStatus.AwaitingApproval)
            {
                job.CustomerApproved = true;
                job.ApprovedAtUtc = DateTime.UtcNow;
                job.CustomerSignatureImageDataUrl = signatureDataUrl;
                job.Status = JobStatus.InProgress;
                job.StatusChangedUtc = DateTime.UtcNow;
                db.SaveChanges();
                TempData["Success"] = "Quote approved - work will begin shortly.";
            }

            return RedirectToAction("MyBookings");
        }

        // POST: /JobCard/Decline/5 - customer declines the quote and signs.
        // Also requires a signature, and an optional reason for our records.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Customer)]
        public ActionResult Decline(int id, string signatureDataUrl, string declineReason)
        {
            var job = db.JobCards.FirstOrDefault(j => j.Id == id && j.CustomerId == CurrentUserId);
            if (job == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(signatureDataUrl))
            {
                TempData["Error"] = "Please sign before declining the quote.";
                return RedirectToAction("MyBookings");
            }

            if (job.Status == JobStatus.AwaitingApproval)
            {
                job.CustomerDeclined = true;
                job.DeclinedAtUtc = DateTime.UtcNow;
                job.DeclineReason = declineReason;
                job.CustomerSignatureImageDataUrl = signatureDataUrl;
                job.Status = JobStatus.Cancelled;
                job.StatusChangedUtc = DateTime.UtcNow;
                db.SaveChanges();
                TempData["Success"] = "Quote declined. The vehicle can be collected without further work.";
            }

            return RedirectToAction("MyBookings");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
