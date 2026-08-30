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

        // ---------- Receptionist / Manager: create a job card ----------

        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Create(int? vehicleId)
        {
            ViewBag.Vehicles = db.Vehicles.Include(v => v.Owner).OrderBy(v => v.PlateNumber).ToList();
            var model = new JobCard();
            if (vehicleId.HasValue) model.VehicleId = vehicleId.Value;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Create(JobCard model)
        {
            ModelState.Remove("Vehicle");
            ModelState.Remove("Customer");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("AssignedMechanic");
            ModelState.Remove("ManagerSignedBy");

            var vehicle = db.Vehicles.Find(model.VehicleId);
            if (vehicle == null)
                ModelState.AddModelError("VehicleId", "Select a vehicle.");

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = db.Vehicles.Include(v => v.Owner).OrderBy(v => v.PlateNumber).ToList();
                return View(model);
            }

            model.CustomerId = vehicle.OwnerId;
            model.CreatedByUserId = CurrentUserId.Value;
            model.Status = JobStatus.Booked;
            model.DateBookedUtc = DateTime.UtcNow;
            model.StatusChangedUtc = DateTime.UtcNow;

            db.JobCards.Add(model);
            db.SaveChanges();

            model.JobNumber = "JC-" + model.Id.ToString("D6");
            db.SaveChanges();

            TempData["Success"] = "Job card " + model.JobNumber + " created.";
            return RedirectToAction("Details", new { id = model.Id });
        }

        // ---------- Receptionist / Manager: kanban board ----------

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

        // ---------- Manager: assign a mechanic ----------

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

        // ---------- Mechanic: view assigned jobs ----------

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

        // ---------- Status update (drag-and-drop board + mechanic's own page) ----------

        [HttpPost]
        [RequireRole] // any logged-in user; fine-grained check below
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

        // ---------- Mechanic: save time estimate + quoted amount ----------

        [HttpPost]
        [RequireRole(UserRole.Mechanic)]
        public JsonResult UpdateEstimate(int id, string hours, string amount)
        {
            var job = db.JobCards.Find(id);
            if (job == null)
                return Json(new { success = false, message = "Job card not found." });

            if (job.AssignedMechanicId != CurrentUserId)
                return Json(new { success = false, message = "This job isn't assigned to you." });

            // Parse explicitly with InvariantCulture - HTML number inputs always send
            // period-decimal values (e.g. "150.50") regardless of the browser's
            // locale, but ASP.NET's automatic model binding for decimal parameters
            // uses the SERVER's regional settings, which can silently fail to parse
            // a period-decimal value if that culture expects a comma instead.
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

        // ---------- Manager: sign off and release for pickup ----------

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

        // ---------- Printable job card / details ----------

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

        // ---------- Quotation - generated as soon as a Manager signs off ----------

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

        // ---------- Customer: view their own jobs + approve quotes ----------

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Customer)]
        public ActionResult Approve(int id)
        {
            var job = db.JobCards.FirstOrDefault(j => j.Id == id && j.CustomerId == CurrentUserId);
            if (job == null) return HttpNotFound();

            if (job.Status == JobStatus.AwaitingApproval)
            {
                job.CustomerApproved = true;
                job.ApprovedAtUtc = DateTime.UtcNow;
                job.Status = JobStatus.InProgress;
                job.StatusChangedUtc = DateTime.UtcNow;
                db.SaveChanges();
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