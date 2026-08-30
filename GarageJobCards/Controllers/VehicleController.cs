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
        public ActionResult Create(int? ownerId)
        {
            ViewBag.Customers = db.Users.Where(u => u.Role == UserRole.Customer).OrderBy(u => u.FullName).ToList();
            var vehicle = new Vehicle();
            if (ownerId.HasValue) vehicle.OwnerId = ownerId.Value;
            return View(vehicle);
        }

        // POST: /Vehicle/Create
        // Registers the vehicle AND creates its first job card in one step -
        // "ServiceRequested" comes from the same form, it just isn't part of
        // the Vehicle model itself. The quoted amount is NOT set here - the
        // mechanic sets it from their dashboard once they've actually
        // diagnosed the car, not the receptionist at intake.
        //
        // IMPORTANT: this parameter must NOT be named "model" - Vehicle has a
        // property called "Model" (the car's model name), and MVC's binder
        // matches parameter/field names case-insensitively. A parameter named
        // "model" collides with the "Model" form field and makes the binder
        // try to convert that single field's text into the whole Vehicle
        // object, producing a "the value 'X' is invalid" error. "vehicle"
        // avoids the collision entirely.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Vehicle vehicle, string ServiceRequested)
        {
            ModelState.Remove("Owner");

            // Vehicle's own [Required]/[StringLength]/[RegularExpression]/[Range]
            // attributes (Make, Model, PlateNumber, Mileage) are checked
            // automatically here since "vehicle" is bound directly from the
            // form - ModelState.IsValid below covers all of them.

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
            db.SaveChanges(); // generates vehicle.Id

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
            db.SaveChanges(); // generates job.Id

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
