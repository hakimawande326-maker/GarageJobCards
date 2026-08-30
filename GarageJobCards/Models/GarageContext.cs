using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using GarageJobCards.Infrastructure;

namespace GarageJobCards.Models
{
 public class GarageContext : DbContext
 {
 // Matches the connection string in Web.config - see README.
 public GarageContext() : base("name=GarageContext") { }

 public DbSet<User> Users { get; set; }
 public DbSet<Vehicle> Vehicles { get; set; }
 public DbSet<JobCard> JobCards { get; set; }
 public DbSet<UserChangeRequest> UserChangeRequests { get; set; }

 protected override void OnModelCreating(DbModelBuilder modelBuilder)
 {
 modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

 // JobCard has 3 separate FKs into User (Customer, CreatedByUser,
 // AssignedMechanic). SQL Server won't allow cascade delete on more
 // than one of these at once, so we turn cascade off for all of them - 
 // deleting a user should never silently delete job history.
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

 // UserChangeRequest has 3 separate FKs into User (Target, RequestedBy,
 // ReviewedBy) - same multi-FK situation as JobCard, cascade off for all.
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

 base.OnModelCreating(modelBuilder);
 }
 }

 // Recreates the DB whenever the model changes - fine for development.
 // Switch to a Migrations-based initializer before going to production.
 public class GarageContextInitializer : DropCreateDatabaseIfModelChanges<GarageContext>
 {
 protected override void Seed(GarageContext context)
 {
 string hash, salt;

 // --- Staff accounts (password for all of them: "Password1") ---
 PasswordHelper.CreateHash("Password1", out hash, out salt);

 var receptionist = new User { FullName = "Nomvula Mthembu", Email = "reception@philasauto.co.za", Phone = "0721234567", Role = UserRole.Receptionist, PasswordHash = hash, PasswordSalt = salt };
 var manager = new User { FullName = "Bheki Khumalo", Email = "manager@philasauto.co.za", Phone = "0721234568", Role = UserRole.Manager, PasswordHash = hash, PasswordSalt = salt };
 var mechanic1 = new User { FullName = "Sipho Dube", Email = "sdube@philasauto.co.za", Phone = "0721234569", Role = UserRole.Mechanic, PasswordHash = hash, PasswordSalt = salt };
 var mechanic2 = new User { FullName = "Mandla Cele", Email = "mcele@philasauto.co.za", Phone = "0721234570", Role = UserRole.Mechanic, PasswordHash = hash, PasswordSalt = salt };

 // --- Sample customers (same demo password) ---
 var customer1 = new User { FullName = "Thandeka Ndlovu", Email = "thandeka@example.com", Phone = "0765551234", Address = "12 Mangosuthu Highway", City = "Umlazi", PostalCode = "4066", Role = UserRole.Customer, PasswordHash = hash, PasswordSalt = salt };
 var customer2 = new User { FullName = "Sipho Zulu", Email = "sipho.zulu@example.com", Phone = "0839876543", Address = "45 Bhekuzulu Road", City = "Umlazi", PostalCode = "4066", Role = UserRole.Customer, PasswordHash = hash, PasswordSalt = salt };

 context.Users.AddRange(new[] { receptionist, manager, mechanic1, mechanic2, customer1, customer2 });
 context.SaveChanges(); // generates Ids

 // --- Sample vehicles ---
 var vehicle1 = new Vehicle { OwnerId = customer1.Id, Make = "VW", Model = "Golf", PlateNumber = "ND 78 EF GP", Mileage = 61000 };
 var vehicle2 = new Vehicle { OwnerId = customer2.Id, Make = "Toyota", Model = "Hilux", PlateNumber = "ND 45 CD GP", Mileage = 152300 };

 context.Vehicles.AddRange(new[] { vehicle1, vehicle2 });
 context.SaveChanges();

 // --- Sample job cards ---
 var job1 = new JobCard
 {
 VehicleId = vehicle1.Id,
 CustomerId = customer1.Id,
 CreatedByUserId = receptionist.Id,
 AssignedMechanicId = mechanic1.Id,
 ServiceRequested = "Gearbox slipping between 2nd and 3rd.",
 // EstimateAmount intentionally left unset - mechanic sets this now, not seed data
 Status = JobStatus.InProgress,
 DateBookedUtc = DateTime.UtcNow.AddDays(-1),
 StatusChangedUtc = DateTime.UtcNow.AddHours(-3),
 CustomerApproved = true,
 ApprovedAtUtc = DateTime.UtcNow.AddHours(-20)
 };

 var job2 = new JobCard
 {
 VehicleId = vehicle2.Id,
 CustomerId = customer2.Id,
 CreatedByUserId = receptionist.Id,
 ServiceRequested = "Full service + oil change, 21-point inspection.",
 // EstimateAmount intentionally left unset - mechanic sets this now, not seed data
 Status = JobStatus.AwaitingApproval,
 DateBookedUtc = DateTime.UtcNow.AddHours(-6),
 StatusChangedUtc = DateTime.UtcNow.AddHours(-1)
 };

 context.JobCards.AddRange(new[] { job1, job2 });
 context.SaveChanges();

 job1.JobNumber = "JC-" + job1.Id.ToString("D6");
 job2.JobNumber = "JC-" + job2.Id.ToString("D6");
 context.SaveChanges();
 }
 }
}
