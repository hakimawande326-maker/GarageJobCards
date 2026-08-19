using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
 [RequireRole(UserRole.Manager)]
 public class AnalyticsController : BaseController
 {
 private readonly GarageContext db = new GarageContext();

 public ActionResult Index()
 {
 var allJobs = db.JobCards.Include(j => j.AssignedMechanic).ToList();
 var completedJobs = allJobs.Where(j => j.DateCompletedUtc != null).ToList();
 var activeJobs = allJobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
 var cancelledJobs = allJobs.Where(j => j.Status == JobStatus.Cancelled).ToList();

 var mechanics = db.Users.Where(u => u.Role == UserRole.Mechanic).ToList();

 var model = new AnalyticsViewModel
 {
 TotalJobs = allJobs.Count,
 ActiveJobs = activeJobs.Count,
 CompletedJobs = completedJobs.Count,
 CancelledJobs = cancelledJobs.Count,
 TotalRevenue = completedJobs.Sum(j => j.EstimateAmount ?? 0),
 AvgTurnaroundHours = completedJobs.Any()
 ? completedJobs.Average(j => (j.DateCompletedUtc.Value - j.DateBookedUtc).TotalHours)
 : 0,
 TotalCustomers = db.Users.Count(u => u.Role == UserRole.Customer),
 TotalVehicles = db.Vehicles.Count(),
 TotalMechanics = mechanics.Count,

 StatusBreakdown = JobCard.TimelineOrder
 .Select(s => new StatusCount
 {
 Status = s,
 Count = activeJobs.Count(j => j.Status == s)
 })
 .ToList(),

 MechanicPerformance = mechanics
 .Select(m => new MechanicStat
 {
 Name = m.FullName,
 ActiveCount = activeJobs.Count(j => j.AssignedMechanicId == m.Id),
 CompletedCount = completedJobs.Count(j => j.AssignedMechanicId == m.Id)
 })
 .OrderByDescending(x => x.ActiveCount)
 .ToList()
 };

 return View(model);
 }

 protected override void Dispose(bool disposing)
 {
 if (disposing) db.Dispose();
 base.Dispose(disposing);
 }
 }
}
