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

 // Mechanics may only update jobs assigned to them.
 if (CurrentUserRole == UserRole.Mechanic && job.AssignedMechanicId != CurrentUserId)
 return Json(new { success = false, message = "This job isn't assigned to you." });

 // Customers can never move a job through the board directly.
 if (CurrentUserRole == UserRole.Customer)
 return Json(new { success = false, message = "Not permitted." });

 job.Status = status;
 job.StatusChangedUtc = DateTime.UtcNow;
 if (status == JobStatus.Completed)
 job.DateCompletedUtc = DateTime.UtcNow;

 db.SaveChanges();
 return Json(new { success = true, status = job.Status.ToString(), label = JobCard.StatusLabel(job.Status) });
 }

 // ---------- Mechanic: save a time estimate ----------

 [HttpPost]
 [RequireRole(UserRole.Mechanic)]
 public JsonResult UpdateEstimate(int id, decimal hours)
 {
 var job = db.JobCards.Find(id);
 if (job == null)
 return Json(new { success = false, message = "Job card not found." });

 if (job.AssignedMechanicId != CurrentUserId)
 return Json(new { success = false, message = "This job isn't assigned to you." });

 job.EstimatedHours = hours;
 db.SaveChanges();

 return Json(new
 {
 success = true,
 hours = job.EstimatedHours,
 estimatedCompletion = job.EstimatedCompletionUtc.HasValue
 ? job.EstimatedCompletionUtc.Value.ToLocalTime().ToString("dd MMM, HH:mm")
 : null
 });
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
