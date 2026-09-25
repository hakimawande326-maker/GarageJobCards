using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace GarageJobCards.Models
{
    public class GarageContext : DbContext
    {
        public GarageContext() : base("name=GarageContext") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<JobCard> JobCards { get; set; }
        public DbSet<UserChangeRequest> UserChangeRequests { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
 public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DiagnosticReport> DiagnosticReports { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<JobCard>()
                .HasRequired(j => j.Customer)
                .WithMany()
                .HasForeignKey(j => j.CustomerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<JobCard>()
                .HasRequired(j => j.CreatedByUser)
                .WithMany()
                .HasForeignKey(j => j.CreatedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<JobCard>()
                .HasOptional(j => j.AssignedMechanic)
                .WithMany()
                .HasForeignKey(j => j.AssignedMechanicId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<JobCard>()
                .HasOptional(j => j.ManagerSignedBy)
                .WithMany()
                .HasForeignKey(j => j.ManagerSignedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<JobCard>()
                .HasRequired(j => j.Vehicle)
                .WithMany()
                .HasForeignKey(j => j.VehicleId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Vehicle>()
                .HasRequired(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.OwnerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserChangeRequest>()
                .HasRequired(r => r.TargetUser)
                .WithMany()
                .HasForeignKey(r => r.TargetUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserChangeRequest>()
                .HasRequired(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserChangeRequest>()
                .HasOptional(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .WillCascadeOnDelete(false);

            // Purchases - a walk-in customer is allowed (CustomerId nullable),
            // and cascade is off everywhere to protect sales history.
            modelBuilder.Entity<Purchase>()
                .HasOptional(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Purchase>()
                .HasOptional(p => p.ProcessedByUser)
                .WithMany()
                .HasForeignKey(p => p.ProcessedByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Purchase>()
                .HasOptional(p => p.JobCard)
                .WithMany()
                .HasForeignKey(p => p.JobCardId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PurchaseItem>()
                .HasRequired(pi => pi.Purchase)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PurchaseId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PurchaseItem>()
                .HasRequired(pi => pi.Product)
                .WithMany()
                .HasForeignKey(pi => pi.ProductId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Delivery>()
                .HasRequired(d => d.Purchase)
                .WithMany()
                .HasForeignKey(d => d.PurchaseId)
                .WillCascadeOnDelete(false);

     modelBuilder.Entity<Delivery>()
                .HasRequired(d => d.Driver)
                .WithMany()
                .HasForeignKey(d => d.DriverUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiagnosticReport>()
                .HasRequired(r => r.JobCard)
                .WithMany()
                .HasForeignKey(r => r.JobCardId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiagnosticReport>()
                .HasRequired(r => r.WrittenBy)
                .WithMany()
                .HasForeignKey(r => r.WrittenByUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiagnosticReport>()
                .HasOptional(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }

}
