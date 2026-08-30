using System;
using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public enum JobStatus
    {
        Booked = 0,
        DiagnosticsInProgress = 1,
        AwaitingApproval = 2,
        InProgress = 3,
        QualityCheck = 4,
        ReadyForPickup = 5,
        Completed = 6,
        Cancelled = 7
    }

    public class JobCard
    {
        public int Id { get; set; }

        // Human-friendly reference shown to the customer, e.g. "JC-000123"
        public string JobNumber { get; set; }

        public int VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }

        public int CustomerId { get; set; } // FK -> User.Id (Role = Customer)
        public virtual User Customer { get; set; }

        public int CreatedByUserId { get; set; } // FK -> User.Id (Role = Receptionist)
        public virtual User CreatedByUser { get; set; }

        public int? AssignedMechanicId { get; set; } // FK -> User.Id (Role = Mechanic)
        public virtual User AssignedMechanic { get; set; }

        [Required(ErrorMessage = "Describe what the customer needs done.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Give a meaningful description - at least 10 characters.")]
        [Display(Name = "What's wrong / service requested")]
        public string ServiceRequested { get; set; }

        [Range(0.01, 500000, ErrorMessage = "Quoted amount must be between R0.01 and R500,000.")]
        [Display(Name = "Quoted amount (R)")]
        public decimal? EstimateAmount { get; set; }

        // Set by the mechanic once they've assessed the job - how many hours
        // of work it'll take from the point they start.
        [Range(0.1, 500, ErrorMessage = "Estimated hours must be between 0.1 and 500.")]
        [Display(Name = "Estimated repair time (hours)")]
        public decimal? EstimatedHours { get; set; }

        public JobStatus Status { get; set; }

        [Display(Name = "Internal notes")]
        [StringLength(1000)]
        public string Notes { get; set; }

        public bool CustomerApproved { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        // Set when a Manager signs off the job as ready for pickup.
        public int? ManagerSignedByUserId { get; set; }
        public virtual User ManagerSignedBy { get; set; }
        public DateTime? SignedAtUtc { get; set; }

        public DateTime DateBookedUtc { get; set; }
        public DateTime? DateCompletedUtc { get; set; }
        public DateTime StatusChangedUtc { get; set; }

        public JobCard()
        {
            DateBookedUtc = DateTime.UtcNow;
            StatusChangedUtc = DateTime.UtcNow;
            Status = JobStatus.Booked;
        }

        public static readonly JobStatus[] TimelineOrder =
        {
            JobStatus.Booked,
            JobStatus.DiagnosticsInProgress,
            JobStatus.AwaitingApproval,
            JobStatus.InProgress,
            JobStatus.QualityCheck,
            JobStatus.ReadyForPickup,
            JobStatus.Completed
        };

        // Estimated completion = whenever work actually started (last status
        // change) plus however many hours the mechanic estimated. Returns
        // null until a mechanic has entered an estimate.
        public DateTime? EstimatedCompletionUtc
        {
            get
            {
                if (!EstimatedHours.HasValue) return null;
                var startPoint = Status == JobStatus.InProgress || Status == JobStatus.QualityCheck
                    ? StatusChangedUtc
                    : DateTime.UtcNow;
                return startPoint.AddHours((double)EstimatedHours.Value);
            }
        }

        // South African VAT at 15% - EstimateAmount is treated as the
        // Quotation total: labor/parts + a flat admin fee, then VAT applied
        // on top of both (the admin fee is a taxable service charge too).
        public const decimal AdminFee = 25m;

        public decimal SubtotalExclVat
        {
            get { return EstimateAmount ?? 0; }
        }

        public decimal TaxableAmount
        {
            get { return SubtotalExclVat + AdminFee; }
        }

        public decimal VatAmount
        {
            get { return Math.Round(TaxableAmount * 0.15m, 2); }
        }

        public decimal TotalInclVat
        {
            get { return TaxableAmount + VatAmount; }
        }

        public string QuotationNumber
        {
            get { return "QN-" + Id.ToString("D6"); }
        }

        public static string StatusLabel(JobStatus status)
        {
            switch (status)
            {
                case JobStatus.Booked: return "Booked";
                case JobStatus.DiagnosticsInProgress: return "Diagnostics In Progress";
                case JobStatus.AwaitingApproval: return "Awaiting Approval";
                case JobStatus.InProgress: return "Work In Progress";
                case JobStatus.QualityCheck: return "Quality Check";
                case JobStatus.ReadyForPickup: return "Ready For Pickup";
                case JobStatus.Completed: return "Completed";
                case JobStatus.Cancelled: return "Cancelled";
                default: return status.ToString();
            }
        }
    }
}
