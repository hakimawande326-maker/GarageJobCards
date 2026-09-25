using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public int CustomerId { get; set; }
        public virtual User Customer { get; set; }

        public int CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; }

        public int? AssignedMechanicId { get; set; }
        public virtual User AssignedMechanic { get; set; }

        [Required(ErrorMessage = "Describe what the customer needs done.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Give a meaningful description - at least 10 characters.")]
        [Display(Name = "What's wrong / service requested")]
        public string ServiceRequested { get; set; }

        [Range(0.01, 500000, ErrorMessage = "Quoted amount must be between R0.01 and R500,000.")]
        [Display(Name = "Quoted amount (R)")]
        public decimal? EstimateAmount { get; set; }

        // Set by the mechanic once they've assessed the job - how many hours
        // of work it'll take from the point the customer approves the quote.
        [Range(0.1, 500, ErrorMessage = "Estimated hours must be between 0.1 and 500.")]
        [Display(Name = "Estimated repair time (hours)")]
        public decimal? EstimatedHours { get; set; }

        public JobStatus Status { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        // ---------- Customer's response to the quote ----------
        // A customer must explicitly approve OR decline a quote before work
        // (beyond diagnostics) proceeds - both require a drawn signature,
        // same mechanism as the Manager's sign-off.
        public bool CustomerApproved { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        public bool CustomerDeclined { get; set; }
        public DateTime? DeclinedAtUtc { get; set; }
        [StringLength(500)]
        public string DeclineReason { get; set; }

        // Base64 PNG data URL of the customer's drawn signature, captured at
        // the moment they approve or decline the quote.
        public string CustomerSignatureImageDataUrl { get; set; }

        public int? ManagerSignedByUserId { get; set; }
        public virtual User ManagerSignedBy { get; set; }
        public DateTime? SignedAtUtc { get; set; }

        public DateTime DateBookedUtc { get; set; }
        public DateTime? DateCompletedUtc { get; set; }
        public DateTime StatusChangedUtc { get; set; }

 // Quotation total: labor/parts + a flat admin fee, then VAT applied
        // on top of both (the admin fee is a taxable service charge too).
        public const decimal AdminFee = 25m;

        // Every job gets diagnosed regardless of what the customer decides
        // afterward - so this fee always applies, just differently:
        //  - Decline the repair: pay the full diagnostic fee, since that's
        //    the only work that happened.
        //  - Accept the repair: get 30% off the diagnostic fee as a
        //    goodwill discount, folded into the overall repair total.
        public const decimal DiagnosticFee = 1080m;
        public const decimal DiagnosticDiscountPercent = 0.30m;

        public decimal DiagnosticFeeCharged
        {
            get
            {
                if (CustomerDeclined) return DiagnosticFee;
                if (CustomerApproved) return Math.Round(DiagnosticFee * (1m - DiagnosticDiscountPercent), 2);
                return 0m; // customer hasn't decided yet - not charged until they do
            }
        }

        public decimal SubtotalExclVat
        {
            // If declined, no repair work happens - only the diagnostic fee applies.
            get { return CustomerDeclined ? 0m : (EstimateAmount ?? 0m); }
        }

        public decimal TaxableAmount
        {
            get { return SubtotalExclVat + AdminFee + DiagnosticFeeCharged; }
        }

        public decimal VatAmount
        {
            get { return Math.Round(TaxableAmount * 0.15m, 2); }
        }

 public decimal TotalInclVat
        {
            get { return TaxableAmount + VatAmount; }
        }

        // Shown to the customer BEFORE they decide, so they know exactly
        // what each choice costs - the live properties above only reflect
        // the real outcome once CustomerApproved/CustomerDeclined is
        // actually set, which hasn't happened yet at that point.
        public decimal EstimatedTotalIfApproved
        {
            get
            {
                var discountedDiagnostic = Math.Round(DiagnosticFee * (1m - DiagnosticDiscountPercent), 2);
                var taxable = (EstimateAmount ?? 0m) + AdminFee + discountedDiagnostic;
                return taxable + Math.Round(taxable * 0.15m, 2);
            }
        }

        public decimal EstimatedTotalIfDeclined
        {
            get
            {
                var taxable = DiagnosticFee;
                return taxable + Math.Round(taxable * 0.15m, 2);
            }
        }

        public string QuotationNumber
        {
            get { return "QN-" + Id.ToString("D6"); }
        }

        // Estimated finish time: work officially starts once the customer
        // approves the quote, so completion = approval time + quoted hours.
        // Used throughout the app to show customers a concrete "ready by"
        // time instead of a vague status, and helps staff plan workload.
        public DateTime? EstimatedCompletionUtc
        {
            get
            {
                if (!EstimatedHours.HasValue || !ApprovedAtUtc.HasValue) return null;
                return ApprovedAtUtc.Value.AddHours((double)EstimatedHours.Value);
            }
        }

        public static readonly JobStatus[] TimelineOrder = new[]
        {
            JobStatus.Booked,
            JobStatus.DiagnosticsInProgress,
            JobStatus.AwaitingApproval,
            JobStatus.InProgress,
            JobStatus.QualityCheck,
            JobStatus.ReadyForPickup,
            JobStatus.Completed
        };

        public static string StatusLabel(JobStatus status)
        {
            switch (status)
            {
                case JobStatus.Booked: return "Booked";
                case JobStatus.DiagnosticsInProgress: return "Diagnostics";
                case JobStatus.AwaitingApproval: return "Awaiting Approval";
                case JobStatus.InProgress: return "In Progress";
                case JobStatus.QualityCheck: return "Quality Check";
                case JobStatus.ReadyForPickup: return "Ready for Pickup";
                case JobStatus.Completed: return "Completed";
                case JobStatus.Cancelled: return "Cancelled";
                default: return status.ToString();
            }
        }
    }
}
