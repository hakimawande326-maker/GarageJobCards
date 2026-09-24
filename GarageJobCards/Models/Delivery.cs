using System;
using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public enum DeliveryStatus
    {
        Pending = 0,
        OutForDelivery = 1,
        Delivered = 2,
        Cancelled = 3
    }

    // An in-house delivery of parts from a Purchase - driven by one of our
    // own staff, not a third-party courier. The driver's phone reports its
    // live GPS position via the browser's own location API while the
    // "Drive" page is open, and the customer watches that same position
    // update on a map in near-real-time.
    public class Delivery
    {
        public int Id { get; set; }

        public string DeliveryNumber { get; set; } // e.g. "DL-000045"

        public int PurchaseId { get; set; }
        public virtual Purchase Purchase { get; set; }

        // The staff member physically driving this delivery - any
        // Receptionist, Mechanic, or Manager can be assigned, since this is
        // an in-house delivery, not a specialised courier role.
        public int DriverUserId { get; set; }
        public virtual User Driver { get; set; }

        [Required(ErrorMessage = "A delivery address is required.")]
        [StringLength(300, MinimumLength = 5)]
        public string DeliveryAddress { get; set; }

        public DeliveryStatus Status { get; set; }

        // Last reported GPS position of the driver - null until they start
        // driving and their phone reports a first fix.
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdateUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DepartedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }

        public Delivery()
        {
            CreatedAtUtc = DateTime.UtcNow;
            Status = DeliveryStatus.Pending;
        }

        public static string StatusLabel(DeliveryStatus status)
        {
            switch (status)
            {
                case DeliveryStatus.Pending: return "Preparing";
                case DeliveryStatus.OutForDelivery: return "Out for Delivery";
                case DeliveryStatus.Delivered: return "Delivered";
                case DeliveryStatus.Cancelled: return "Cancelled";
                default: return status.ToString();
            }
        }
    }
}
