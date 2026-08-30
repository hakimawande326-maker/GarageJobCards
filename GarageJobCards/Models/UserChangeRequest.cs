using System;
using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public enum ChangeRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    // A Receptionist can edit a customer's contact details (phone, address)
    // directly, but changing their name or email needs a Manager's sign-off.
    // This records that request until a Manager approves or rejects it.
    public class UserChangeRequest
    {
        public int Id { get; set; }

        public int TargetUserId { get; set; }
        public virtual User TargetUser { get; set; }

        [Required, StringLength(40)]
        public string FieldName { get; set; } // "FullName" or "Email"

        [StringLength(120)]
        public string OldValue { get; set; }

        [Required, StringLength(120)]
        public string NewValue { get; set; }

        public int RequestedByUserId { get; set; }
        public virtual User RequestedBy { get; set; }
        public DateTime RequestedAtUtc { get; set; }

        public ChangeRequestStatus Status { get; set; }

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedBy { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }

        public UserChangeRequest()
        {
            RequestedAtUtc = DateTime.UtcNow;
            Status = ChangeRequestStatus.Pending;
        }
    }
}
