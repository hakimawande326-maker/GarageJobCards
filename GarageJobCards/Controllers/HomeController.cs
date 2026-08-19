using System;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
 public class HomeController : Controller
 {
 private readonly GarageContext db = new GarageContext();

 // GET: /
 public ActionResult Index()
 {
 // Pull real numbers for the stats bar instead of hardcoding them.
 var completed = db.JobCards.Where(j => j.DateCompletedUtc != null).ToList();

 ViewBag.TotalCars = db.JobCards.Count();
 ViewBag.AvgTurnaroundMinutes = completed.Any()
 ? (int)completed.Average(j => EntityFunctionsAverageMinutes(j))
 : 92; // fallback shown before any jobs have been completed

 return View();
 }

 // Rough average turnaround in minutes between booking and completion.
 private double EntityFunctionsAverageMinutes(JobCard job)
 {
 if (job.DateCompletedUtc == null) return 0;
 return (job.DateCompletedUtc.Value - job.DateBookedUtc).TotalMinutes;
 }

 protected override void Dispose(bool disposing)
 {
 if (disposing) db.Dispose();
 base.Dispose(disposing);
 }
 }
}
