using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using GarageJobCards.Infrastructure;

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

            base.OnModelCreating(modelBuilder);
        }
    }

    public class GarageContextInitializer : DropCreateDatabaseIfModelChanges<GarageContext>
    {
        protected override void Seed(GarageContext context)
        {
            string hash, salt;
            PasswordHelper.CreateHash("Password1!", out hash, out salt);

            var receptionist = new User { FullName = "Nomvula Mthembu", Email = "reception@philasauto.co.za", Phone = "0721234567", Role = UserRole.Receptionist, PasswordHash = hash, PasswordSalt = salt };
            var manager = new User { FullName = "Bheki Khumalo", Email = "manager@philasauto.co.za", Phone = "0721234568", Role = UserRole.Manager, PasswordHash = hash, PasswordSalt = salt };
            var mechanic1 = new User { FullName = "Sipho Dube", Email = "sdube@philasauto.co.za", Phone = "0721234569", Role = UserRole.Mechanic, PasswordHash = hash, PasswordSalt = salt };
            var mechanic2 = new User { FullName = "Mandla Cele", Email = "mcele@philasauto.co.za", Phone = "0721234570", Role = UserRole.Mechanic, PasswordHash = hash, PasswordSalt = salt };
            var driver1 = new User { FullName = "Themba Ngcobo", Email = "driver@philasauto.co.za", Phone = "0721234571", Role = UserRole.Driver, PasswordHash = hash, PasswordSalt = salt };

            var customer1 = new User { FullName = "Thandeka Ndlovu", Email = "thandeka@example.com", Phone = "0765551234", Address = "12 Mangosuthu Highway", City = "Umlazi", PostalCode = "4066", DriversLicenseNumber = "TN123456", Role = UserRole.Customer, PasswordHash = hash, PasswordSalt = salt };
            var customer2 = new User { FullName = "Sipho Zulu", Email = "sipho.zulu@example.com", Phone = "0839876543", Address = "45 Bhekuzulu Road", City = "Umlazi", PostalCode = "4066", DriversLicenseNumber = "SZ654321", Role = UserRole.Customer, PasswordHash = hash, PasswordSalt = salt };

            context.Users.AddRange(new[] { receptionist, manager, mechanic1, mechanic2, driver1, customer1, customer2 });
            context.SaveChanges();

            // Thandeka has TWO vehicles, showing a customer can bring in multiple.
            var vehicle1 = new Vehicle { OwnerId = customer1.Id, Make = "VW", Model = "Golf", Colour = "White", VinNumber = "WVWZZZ1KZAW123456", PlateNumber = "ND 78 EF GP", Mileage = 61000 };
            var vehicle1b = new Vehicle { OwnerId = customer1.Id, Make = "Toyota", Model = "Corolla", Colour = "Silver", VinNumber = "JTDBR32E720123456", PlateNumber = "ND 12 AB GP", Mileage = 34000 };
            var vehicle2 = new Vehicle { OwnerId = customer2.Id, Make = "Toyota", Model = "Hilux", Colour = "Grey", VinNumber = "AHTFR22G701234567", PlateNumber = "ND 45 CD GP", Mileage = 152300 };

            context.Vehicles.AddRange(new[] { vehicle1, vehicle1b, vehicle2 });
            context.SaveChanges();

            var job1 = new JobCard
            {
                VehicleId = vehicle1.Id,
                CustomerId = customer1.Id,
                CreatedByUserId = receptionist.Id,
                AssignedMechanicId = mechanic1.Id,
                ServiceRequested = "Gearbox slipping between 2nd and 3rd.",
                Status = JobStatus.InProgress,
                DateBookedUtc = DateTime.UtcNow.AddDays(-1),
                StatusChangedUtc = DateTime.UtcNow.AddHours(-3),
                CustomerApproved = true,
                ApprovedAtUtc = DateTime.UtcNow.AddHours(-20),
                EstimatedHours = 6
            };

            var job2 = new JobCard
            {
                VehicleId = vehicle2.Id,
                CustomerId = customer2.Id,
                CreatedByUserId = receptionist.Id,
                ServiceRequested = "Full service + oil change, 21-point inspection.",
                Status = JobStatus.AwaitingApproval,
                DateBookedUtc = DateTime.UtcNow.AddHours(-6),
                StatusChangedUtc = DateTime.UtcNow.AddHours(-1),
                EstimateAmount = 1850,
                EstimatedHours = 3
            };

            context.JobCards.AddRange(new[] { job1, job2 });
            context.SaveChanges();

            job1.JobNumber = "JC-" + job1.Id.ToString("D6");
            job2.JobNumber = "JC-" + job2.Id.ToString("D6");
            context.SaveChanges();

            // ---------- Parts catalog - a few sample products per category ----------
            var products = new[]
            {
                new Product { Name = "Front Bumper Cover - Golf Mk7", Category = ProductCategory.BumpersAndGrills, Description = "OEM-spec front bumper, unpainted.", Price = 1850m, StockQuantity = 6, PartNumber = "BMP-GF7-001", CompatibleMake = "VW", CompatibleModel = "Golf" },
                new Product { Name = "Radiator Grille - Polo Vivo", Category = ProductCategory.BumpersAndGrills, Description = "Chrome-trim front grille.", Price = 620m, StockQuantity = 10, PartNumber = "GRL-PV-014", CompatibleMake = "VW", CompatibleModel = "Polo Vivo" },

                new Product { Name = "Front Brake Pads (Set)", Category = ProductCategory.BrakesAndClutch, Description = "Ceramic compound, low dust - universal fit.", Price = 480m, StockQuantity = 24, PartNumber = "BRK-PAD-002" },
                new Product { Name = "Clutch Kit - 3 Piece", Category = ProductCategory.BrakesAndClutch, Description = "Plate, cover, and release bearing - universal fit.", Price = 2150m, StockQuantity = 8, PartNumber = "CLT-KIT-3P" },

                new Product { Name = "Radiator - Toyota Hilux", Category = ProductCategory.Radiators, Description = "Aluminium core, direct fit.", Price = 2350m, StockQuantity = 5, PartNumber = "RAD-HLX-009", CompatibleMake = "Toyota", CompatibleModel = "Hilux" },

                new Product { Name = "AC Condenser - Universal", Category = ProductCategory.AirconsCondensersAndFans, Description = "Fits most hatchback models.", Price = 1450m, StockQuantity = 7, PartNumber = "AC-COND-U1" },
                new Product { Name = "Radiator Cooling Fan", Category = ProductCategory.AirconsCondensersAndFans, Description = "12V electric fan, 2-pin - universal fit.", Price = 890m, StockQuantity = 12, PartNumber = "FAN-ELC-12" },

                new Product { Name = "Front Shock Absorber (Each)", Category = ProductCategory.SuspensionAndWheelBearings, Description = "Gas-charged, OEM replacement - universal fit.", Price = 950m, StockQuantity = 16, PartNumber = "SUS-SHK-F1" },
                new Product { Name = "Front Wheel Bearing Kit - Corolla", Category = ProductCategory.SuspensionAndWheelBearings, Description = "Includes hub, bearing, and ABS ring.", Price = 780m, StockQuantity = 14, PartNumber = "WHB-KIT-01", CompatibleMake = "Toyota", CompatibleModel = "Corolla" },

                new Product { Name = "Ignition Coil Pack", Category = ProductCategory.IgnitionAndDoorLocks, Description = "Direct-fit coil-on-plug unit - universal fit.", Price = 620m, StockQuantity = 20, PartNumber = "IGN-COIL-4" },
                new Product { Name = "Door Lock Actuator", Category = ProductCategory.IgnitionAndDoorLocks, Description = "Central locking motor, front left - universal fit.", Price = 410m, StockQuantity = 9, PartNumber = "LCK-ACT-FL" },

                new Product { Name = "Side Mirror - Electric, Left", Category = ProductCategory.DoorMirrors, Description = "Power-adjustable, unpainted - universal fit.", Price = 720m, StockQuantity = 11, PartNumber = "MIR-ELC-L1" },
                new Product { Name = "Side Mirror - Electric, Right", Category = ProductCategory.DoorMirrors, Description = "Power-adjustable, unpainted.", Price = 720m, StockQuantity = 11, PartNumber = "MIR-ELC-R1" },

                new Product { Name = "Front Fender Panel", Category = ProductCategory.FendersDoorsAndBonnets, Description = "Unpainted steel panel, left side.", Price = 1250m, StockQuantity = 6, PartNumber = "FND-PNL-L1" },
                new Product { Name = "Bonnet / Hood Panel", Category = ProductCategory.FendersDoorsAndBonnets, Description = "OEM-spec, unpainted.", Price = 2450m, StockQuantity = 3, PartNumber = "BNT-PNL-01" },

                new Product { Name = "Headlight Assembly - Left", Category = ProductCategory.Lights, Description = "Halogen, includes bulb.", Price = 980m, StockQuantity = 8, PartNumber = "LGT-HDL-L1" },
                new Product { Name = "Tail Light Assembly - Right", Category = ProductCategory.Lights, Description = "LED-style, direct fit.", Price = 750m, StockQuantity = 8, PartNumber = "LGT-TL-R1" },
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}
