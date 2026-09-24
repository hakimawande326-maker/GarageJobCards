using System;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    [RequireRole(UserRole.Receptionist, UserRole.Manager)]
    public class VehicleController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // GET: /Vehicle/Create?ownerId=5
        // ownerId pre-selects the customer (e.g. straight after registering
        // them), but the dropdown lists every customer - so this same screen
        // is also how you add a SECOND (or third...) vehicle for a customer
        // who's already registered and just brought in another car.
        public ActionResult Create(int? ownerId)
        {
            ViewBag.Customers = db.Users.Where(u => u.Role == UserRole.Customer).OrderBy(u => u.FullName).ToList();
            var vehicle = new Vehicle();
            if (ownerId.HasValue) vehicle.OwnerId = ownerId.Value;
            return View(vehicle);
        }

        // POST: /Vehicle/Create
        // "vehicle" must NOT be named "model" - Vehicle has a property called
        // "Model" (the car's model name), and MVC's binder matches
        // parameter/field names case-insensitively, causing a binding
        // collision if the parameter itself is named "model".
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Vehicle vehicle, string ServiceRequested)
        {
            ModelState.Remove("Owner");

            if (string.IsNullOrWhiteSpace(ServiceRequested))
                ModelState.AddModelError("ServiceRequested", "Describe what the customer needs done.");
            else if (ServiceRequested.Trim().Length < 10)
                ModelState.AddModelError("ServiceRequested", "Give a meaningful description - at least 10 characters.");

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = db.Users.Where(u => u.Role == UserRole.Customer).OrderBy(u => u.FullName).ToList();
                ViewBag.ServiceRequested = ServiceRequested;
                return View(vehicle);
            }

            db.Vehicles.Add(vehicle);
            db.SaveChanges();

            var job = new JobCard
            {
                VehicleId = vehicle.Id,
                CustomerId = vehicle.OwnerId,
                CreatedByUserId = CurrentUserId.Value,
                ServiceRequested = ServiceRequested,
                Status = JobStatus.Booked,
                DateBookedUtc = DateTime.UtcNow,
                StatusChangedUtc = DateTime.UtcNow
            };

            db.JobCards.Add(job);
            db.SaveChanges();

            job.JobNumber = "JC-" + job.Id.ToString("D6");
            db.SaveChanges();

            TempData["Success"] = vehicle.Make + " " + vehicle.Model + " (" + vehicle.PlateNumber + ") registered - job card " + job.JobNumber + " created.";
            return RedirectToAction("Details", "JobCard", new { id = job.Id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
