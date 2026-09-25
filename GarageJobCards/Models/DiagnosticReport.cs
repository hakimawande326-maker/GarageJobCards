using System;
using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public enum DiagnosticReportStatus
    {
        PendingApproval = 0,
        Approved = 1,
        NeedsRevision = 2
    }

    // Only created for a job the customer DECLINED - since they're paying
    // the full diagnostic fee without getting a repair, they're owed a
    // proper professional explanation of what was actually found. Written
    // by the mechanic who worked the job, reviewed by a Manager before the
    // customer ever sees it.
    public class DiagnosticReport
    {
        public int Id { get; set; }

        public int JobCardId { get; set; }
        public virtual JobCard JobCard { get; set; }

        public int WrittenByUserId { get; set; }
        public virtual User WrittenBy { get; set; }
        public DateTime WrittenAtUtc { get; set; }

        [Required(ErrorMessage = "Write up what you found before submitting.")]
        [StringLength(3000, MinimumLength = 30, ErrorMessage = "Give a proper professional write-up - at least 30 characters.")]
        [Display(Name = "Diagnostic findings")]
        public string ReportText { get; set; }

        public DiagnosticReportStatus Status { get; set; }

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedBy { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }

        // Only set when a Manager sends it back for revision.
        [StringLength(500)]
        public string ManagerNotes { get; set; }

        public DiagnosticReport()
        {
            WrittenAtUtc = DateTime.UtcNow;
            Status = DiagnosticReportStatus.PendingApproval;
        }
    }
}
