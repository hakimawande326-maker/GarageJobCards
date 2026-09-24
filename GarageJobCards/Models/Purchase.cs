using System;
using System.Collections.Generic;
using System.Linq;

namespace GarageJobCards.Models
{
    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        Eft = 2
    }

    public enum CollectionMethod
    {
        Collect = 0,
        Delivery = 1
    }

    // A single sale transaction - a customer (or walk-in with no account)
    // buying one or more parts at the counter, processed by a Receptionist
    // or Manager. Optionally linked to a job card if the parts were used
    // in a specific repair, rather than sold over the counter standalone.
    public class Purchase
    {
        public int Id { get; set; }

        public string PurchaseNumber { get; set; } // e.g. "PO-000123"

        // Nullable - a walk-in customer buying parts doesn't need an account.
        public int? CustomerId { get; set; }
        public virtual User Customer { get; set; }

        // If the customer has no account, capture a name/phone for the receipt.
        public string WalkInName { get; set; }
        public string WalkInPhone { get; set; }

        // Optional - link this purchase to a job card if these parts were
        // used in a specific repair rather than sold at the counter.
        public int? JobCardId { get; set; }
        public virtual JobCard JobCard { get; set; }

        // Null when the customer checked out and paid online themselves -
        // only set when a Receptionist/Manager rang up an in-person sale.
        public int? ProcessedByUserId { get; set; }
        public virtual User ProcessedByUser { get; set; }

        // How the customer gets their parts, and what that costs. A flat
        // delivery fee for now - distance-based pricing would need a
        // mapping/routing service, a separate feature on its own.
        public CollectionMethod CollectionMethod { get; set; }
 public decimal DeliveryFee { get; set; }

        // Kept here (not just on Delivery) because the Delivery record
        // itself isn't created until payment actually confirms - this is
        // where the chosen address lives in the meantime.
        public string DeliveryAddress { get; set; }

        // True for an online self-checkout by the customer, as opposed to
        // an in-person counter sale processed by staff.
        public bool PaidOnline { get; set; }

        // For online orders, true only once PayFast has actually confirmed
        // payment (via the ITN webhook, or the return redirect as a
        // same-scale fallback for local testing). In-person counter sales
        // are marked paid immediately since payment happens on the spot.
        public bool IsPaid { get; set; }

 public DateTime PurchaseDateUtc { get; set; }

        // How the customer actually paid at the counter - this is a
        // record of a completed in-person payment, not an online gateway.
        public PaymentMethod PaymentMethod { get; set; }

        // Only meaningful for cash - how much they physically handed over,
        // so change owed can be calculated and printed on the receipt.
        public decimal? AmountTendered { get; set; }

        public virtual ICollection<PurchaseItem> Items { get; set; }

 public Purchase()
        {
            PurchaseDateUtc = DateTime.UtcNow;
            Items = new List<PurchaseItem>();
            IsPaid = true; // in-person/staff sales are paid on the spot; online checkout explicitly overrides this to false until PayFast confirms
        }

        public string CustomerDisplayName
        {
            get { return Customer != null ? Customer.FullName : (WalkInName ?? "Walk-in customer"); }
        }

        public decimal SubtotalExclVat
        {
            get { return Items != null ? Items.Sum(i => i.UnitPrice * i.Quantity) : 0; }
        }

        public decimal VatAmount
        {
            get { return Math.Round(SubtotalExclVat * 0.15m, 2); }
        }

        public decimal TotalInclVat
        {
            get { return SubtotalExclVat + VatAmount; }
        }

        // What the customer actually pays - parts total plus delivery fee
        // if they chose delivery (delivery fee itself isn't VAT-taxed).
        public decimal GrandTotal
        {
            get { return TotalInclVat + DeliveryFee; }
        }

        // Only relevant for cash payments - how much to hand back.
        public decimal? ChangeDue
        {
            get { return AmountTendered.HasValue ? AmountTendered.Value - GrandTotal : (decimal?)null; }
        }
    }

    // One line item within a purchase - snapshots the price at time of sale
    // so a later price change doesn't rewrite history.
    public class PurchaseItem
    {
        public int Id { get; set; }

        public int PurchaseId { get; set; }
        public virtual Purchase Purchase { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; } // price at the moment of sale

        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }
}
