using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    public class DeliveryController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // ---------- Staff: set up a delivery for a completed purchase ----------

        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Create(int purchaseId)
        {
            var purchase = db.Purchases.Include(p => p.Customer).FirstOrDefault(p => p.Id == purchaseId);
            if (purchase == null) return HttpNotFound();

            if (db.Deliveries.Any(d => d.PurchaseId == purchaseId))
            {
                TempData["Error"] = "A delivery already exists for this purchase.";
                return RedirectToAction("Receipt", "Product", new { id = purchaseId });
            }

     ViewBag.Purchase = purchase;
            ViewBag.Drivers = db.Users.Where(u => u.Role == UserRole.Driver)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName).ToList();

            var model = new Delivery();
            if (purchase.Customer != null)
                model.DeliveryAddress = purchase.Customer.Address;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Create(int purchaseId, int driverUserId, string deliveryAddress)
        {
            var purchase = db.Purchases.Find(purchaseId);
            if (purchase == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(deliveryAddress) || deliveryAddress.Trim().Length < 5)
            {
                TempData["Error"] = "Enter a valid delivery address.";
                return RedirectToAction("Create", new { purchaseId = purchaseId });
            }

            var delivery = new Delivery
            {
                PurchaseId = purchaseId,
                DriverUserId = driverUserId,
                DeliveryAddress = deliveryAddress,
                Status = DeliveryStatus.Pending
            };

            db.Deliveries.Add(delivery);
            db.SaveChanges();

            delivery.DeliveryNumber = "DL-" + delivery.Id.ToString("D6");
            db.SaveChanges();

            TempData["Success"] = "Delivery " + delivery.DeliveryNumber + " created and assigned.";
            return RedirectToAction("Receipt", "Product", new { id = purchaseId });
        }

        // ---------- Driver: assigned deliveries + live GPS broadcast ----------

        [RequireRole(UserRole.Driver)]
        public ActionResult MyDeliveries()
        {
            var deliveries = db.Deliveries
                .Include(d => d.Purchase)
                .Include(d => d.Purchase.Customer)
                .Where(d => d.DriverUserId == CurrentUserId && d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Cancelled)
                .OrderBy(d => d.CreatedAtUtc)
                .ToList();

            return View(deliveries);
        }

        // GET: /Delivery/Drive/5 - the driver opens this on THEIR OWN PHONE.
        // While this page stays open, their browser's GPS reports position
        // every few seconds via UpdateLocation below.
        [RequireRole(UserRole.Driver)]
        public ActionResult Drive(int id)
        {
            var delivery = db.Deliveries.Include(d => d.Purchase).FirstOrDefault(d => d.Id == id);
            if (delivery == null) return HttpNotFound();
            if (delivery.DriverUserId != CurrentUserId) return new HttpStatusCodeResult(403);

            return View(delivery);
        }

 [HttpPost]
        public JsonResult UpdateLocation(int id, string lat, string lng)
        {
            // Parse explicitly with InvariantCulture - the browser's GPS
            // always sends period-decimal values (e.g. "-29.858"), but
            // automatic model binding for a double parameter uses the
            // SERVER's regional settings, which can silently fail to parse
            // a period-decimal value if that culture expects a comma
            // instead. Same issue we hit with EstimateAmount/EstimatedHours.
            double latValue, lngValue;
            bool hasLat = double.TryParse(lat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out latValue);
            bool hasLng = double.TryParse(lng, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lngValue);

            if (!hasLat || !hasLng)
                return Json(new { success = false, message = "No location received." });

            var delivery = db.Deliveries.Find(id);
            if (delivery == null || delivery.DriverUserId != CurrentUserId)
                return Json(new { success = false });

            delivery.CurrentLatitude = latValue;
            delivery.CurrentLongitude = lngValue;
            delivery.LastLocationUpdateUtc = DateTime.UtcNow;

            if (delivery.Status == DeliveryStatus.Pending)
            {
                delivery.Status = DeliveryStatus.OutForDelivery;
                delivery.DepartedAtUtc = DateTime.UtcNow;
            }

            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
 [RequireRole(UserRole.Driver)]
        public ActionResult MarkDelivered(int id)
        {
            var delivery = db.Deliveries.Find(id);
            if (delivery == null) return HttpNotFound();
            if (delivery.DriverUserId != CurrentUserId) return new HttpStatusCodeResult(403);

            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredAtUtc = DateTime.UtcNow;
            db.SaveChanges();

            TempData["Success"] = delivery.DeliveryNumber + " marked as delivered.";
            return RedirectToAction("MyDeliveries");
        }

        // ---------- Customer: track their delivery on a live map ----------

        [RequireRole(UserRole.Customer)]
        public ActionResult MyOrders()
        {
            var deliveries = db.Deliveries
                .Include(d => d.Purchase)
                .Include(d => d.Driver)
                .Where(d => d.Purchase.CustomerId == CurrentUserId)
                .OrderByDescending(d => d.CreatedAtUtc)
                .ToList();

            return View(deliveries);
        }

        [RequireRole(UserRole.Customer)]
        public ActionResult Track(int id)
        {
            var delivery = db.Deliveries
                .Include(d => d.Purchase)
                .Include(d => d.Driver)
                .FirstOrDefault(d => d.Id == id);

            if (delivery == null) return HttpNotFound();
            if (delivery.Purchase.CustomerId != CurrentUserId) return new HttpStatusCodeResult(403);

            return View(delivery);
        }

        // Polled every few seconds by the customer's Track page to move the
        // marker without a full page reload.
        [RequireRole(UserRole.Customer)]
        public JsonResult GetLocation(int id)
        {
            var delivery = db.Deliveries.Find(id);
            if (delivery == null || delivery.Purchase.CustomerId != CurrentUserId)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                status = delivery.Status.ToString(),
                statusLabel = Delivery.StatusLabel(delivery.Status),
                lat = delivery.CurrentLatitude,
                lng = delivery.CurrentLongitude,
                lastUpdate = delivery.LastLocationUpdateUtc.HasValue
                    ? delivery.LastLocationUpdateUtc.Value.ToLocalTime().ToString("HH:mm:ss")
                    : null
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
