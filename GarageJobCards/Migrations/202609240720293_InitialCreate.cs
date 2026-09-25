namespace GarageJobCards.Models
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Delivery",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DeliveryNumber = c.String(),
                        PurchaseId = c.Int(nullable: false),
                        DriverUserId = c.Int(nullable: false),
                        DeliveryAddress = c.String(nullable: false, maxLength: 300),
                        Status = c.Int(nullable: false),
                        CurrentLatitude = c.Double(),
                        CurrentLongitude = c.Double(),
                        LastLocationUpdateUtc = c.DateTime(),
                        CreatedAtUtc = c.DateTime(nullable: false),
                        DepartedAtUtc = c.DateTime(),
                        DeliveredAtUtc = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.DriverUserId)
                .ForeignKey("dbo.Purchase", t => t.PurchaseId)
                .Index(t => t.PurchaseId)
                .Index(t => t.DriverUserId);
            
            CreateTable(
                "dbo.User",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 80),
                        Email = c.String(nullable: false, maxLength: 120),
                        Phone = c.String(nullable: false),
                        Address = c.String(maxLength: 200),
                        City = c.String(maxLength: 80),
                        PostalCode = c.String(),
                        DriversLicenseNumber = c.String(maxLength: 20),
                        Role = c.Int(nullable: false),
                        PasswordHash = c.String(nullable: false),
                        PasswordSalt = c.String(nullable: false),
                        CreatedAtUtc = c.DateTime(nullable: false),
                        LastLoginUtc = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                        SignatureImageDataUrl = c.String(),
                        ProfilePhotoUrl = c.String(),
                        OtpCode = c.String(),
                        OtpExpiryUtc = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Purchase",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PurchaseNumber = c.String(),
                        CustomerId = c.Int(),
                        WalkInName = c.String(),
                        WalkInPhone = c.String(),
                        JobCardId = c.Int(),
                        ProcessedByUserId = c.Int(),
                        CollectionMethod = c.Int(nullable: false),
                        DeliveryFee = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DeliveryAddress = c.String(),
                        PaidOnline = c.Boolean(nullable: false),
                        IsPaid = c.Boolean(nullable: false),
                        PurchaseDateUtc = c.DateTime(nullable: false),
                        PaymentMethod = c.Int(nullable: false),
                        AmountTendered = c.Decimal(precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.CustomerId)
                .ForeignKey("dbo.JobCard", t => t.JobCardId)
                .ForeignKey("dbo.User", t => t.ProcessedByUserId)
                .Index(t => t.CustomerId)
                .Index(t => t.JobCardId)
                .Index(t => t.ProcessedByUserId);
            
            CreateTable(
                "dbo.PurchaseItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PurchaseId = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Product", t => t.ProductId)
                .ForeignKey("dbo.Purchase", t => t.PurchaseId, cascadeDelete: true)
                .Index(t => t.PurchaseId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Product",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Category = c.Int(nullable: false),
                        Description = c.String(maxLength: 500),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StockQuantity = c.Int(nullable: false),
                        PartNumber = c.String(maxLength: 40),
                        ImageUrl = c.String(),
                        CompatibleMake = c.String(maxLength: 40),
                        CompatibleModel = c.String(maxLength: 40),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.JobCard",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        JobNumber = c.String(),
                        VehicleId = c.Int(nullable: false),
                        CustomerId = c.Int(nullable: false),
                        CreatedByUserId = c.Int(nullable: false),
                        AssignedMechanicId = c.Int(),
                        ServiceRequested = c.String(nullable: false, maxLength: 500),
                        EstimateAmount = c.Decimal(precision: 18, scale: 2),
                        EstimatedHours = c.Decimal(precision: 18, scale: 2),
                        Status = c.Int(nullable: false),
                        Notes = c.String(maxLength: 1000),
                        CustomerApproved = c.Boolean(nullable: false),
                        ApprovedAtUtc = c.DateTime(),
                        CustomerDeclined = c.Boolean(nullable: false),
                        DeclinedAtUtc = c.DateTime(),
                        DeclineReason = c.String(maxLength: 500),
                        CustomerSignatureImageDataUrl = c.String(),
                        ManagerSignedByUserId = c.Int(),
                        SignedAtUtc = c.DateTime(),
                        DateBookedUtc = c.DateTime(nullable: false),
                        DateCompletedUtc = c.DateTime(),
                        StatusChangedUtc = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.AssignedMechanicId)
                .ForeignKey("dbo.User", t => t.CreatedByUserId)
                .ForeignKey("dbo.User", t => t.CustomerId)
                .ForeignKey("dbo.User", t => t.ManagerSignedByUserId)
                .ForeignKey("dbo.Vehicle", t => t.VehicleId)
                .Index(t => t.VehicleId)
                .Index(t => t.CustomerId)
                .Index(t => t.CreatedByUserId)
                .Index(t => t.AssignedMechanicId)
                .Index(t => t.ManagerSignedByUserId);
            
            CreateTable(
                "dbo.Vehicle",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OwnerId = c.Int(nullable: false),
                        Make = c.String(nullable: false, maxLength: 40),
                        Model = c.String(nullable: false, maxLength: 40),
                        Colour = c.String(maxLength: 30),
                        VinNumber = c.String(maxLength: 17),
                        PlateNumber = c.String(nullable: false, maxLength: 20),
                        Mileage = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.OwnerId)
                .Index(t => t.OwnerId);
            
            CreateTable(
                "dbo.DiagnosticReport",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        JobCardId = c.Int(nullable: false),
                        WrittenByUserId = c.Int(nullable: false),
                        WrittenAtUtc = c.DateTime(nullable: false),
                        ReportText = c.String(nullable: false, maxLength: 3000),
                        Status = c.Int(nullable: false),
                        ReviewedByUserId = c.Int(),
                        ReviewedAtUtc = c.DateTime(),
                        ManagerNotes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.JobCard", t => t.JobCardId)
                .ForeignKey("dbo.User", t => t.ReviewedByUserId)
                .ForeignKey("dbo.User", t => t.WrittenByUserId)
                .Index(t => t.JobCardId)
                .Index(t => t.WrittenByUserId)
                .Index(t => t.ReviewedByUserId);
            
            CreateTable(
                "dbo.UserChangeRequest",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TargetUserId = c.Int(nullable: false),
                        FieldName = c.String(nullable: false, maxLength: 40),
                        OldValue = c.String(maxLength: 120),
                        NewValue = c.String(nullable: false, maxLength: 120),
                        RequestedByUserId = c.Int(nullable: false),
                        RequestedAtUtc = c.DateTime(nullable: false),
                        Status = c.Int(nullable: false),
                        ReviewedByUserId = c.Int(),
                        ReviewedAtUtc = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.RequestedByUserId)
                .ForeignKey("dbo.User", t => t.ReviewedByUserId)
                .ForeignKey("dbo.User", t => t.TargetUserId)
                .Index(t => t.TargetUserId)
                .Index(t => t.RequestedByUserId)
                .Index(t => t.ReviewedByUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserChangeRequest", "TargetUserId", "dbo.User");
            DropForeignKey("dbo.UserChangeRequest", "ReviewedByUserId", "dbo.User");
            DropForeignKey("dbo.UserChangeRequest", "RequestedByUserId", "dbo.User");
            DropForeignKey("dbo.DiagnosticReport", "WrittenByUserId", "dbo.User");
            DropForeignKey("dbo.DiagnosticReport", "ReviewedByUserId", "dbo.User");
            DropForeignKey("dbo.DiagnosticReport", "JobCardId", "dbo.JobCard");
            DropForeignKey("dbo.Delivery", "PurchaseId", "dbo.Purchase");
            DropForeignKey("dbo.Purchase", "ProcessedByUserId", "dbo.User");
            DropForeignKey("dbo.Purchase", "JobCardId", "dbo.JobCard");
            DropForeignKey("dbo.JobCard", "VehicleId", "dbo.Vehicle");
            DropForeignKey("dbo.Vehicle", "OwnerId", "dbo.User");
            DropForeignKey("dbo.JobCard", "ManagerSignedByUserId", "dbo.User");
            DropForeignKey("dbo.JobCard", "CustomerId", "dbo.User");
            DropForeignKey("dbo.JobCard", "CreatedByUserId", "dbo.User");
            DropForeignKey("dbo.JobCard", "AssignedMechanicId", "dbo.User");
            DropForeignKey("dbo.PurchaseItem", "PurchaseId", "dbo.Purchase");
            DropForeignKey("dbo.PurchaseItem", "ProductId", "dbo.Product");
            DropForeignKey("dbo.Purchase", "CustomerId", "dbo.User");
            DropForeignKey("dbo.Delivery", "DriverUserId", "dbo.User");
            DropIndex("dbo.UserChangeRequest", new[] { "ReviewedByUserId" });
            DropIndex("dbo.UserChangeRequest", new[] { "RequestedByUserId" });
            DropIndex("dbo.UserChangeRequest", new[] { "TargetUserId" });
            DropIndex("dbo.DiagnosticReport", new[] { "ReviewedByUserId" });
            DropIndex("dbo.DiagnosticReport", new[] { "WrittenByUserId" });
            DropIndex("dbo.DiagnosticReport", new[] { "JobCardId" });
            DropIndex("dbo.Vehicle", new[] { "OwnerId" });
            DropIndex("dbo.JobCard", new[] { "ManagerSignedByUserId" });
            DropIndex("dbo.JobCard", new[] { "AssignedMechanicId" });
            DropIndex("dbo.JobCard", new[] { "CreatedByUserId" });
            DropIndex("dbo.JobCard", new[] { "CustomerId" });
            DropIndex("dbo.JobCard", new[] { "VehicleId" });
            DropIndex("dbo.PurchaseItem", new[] { "ProductId" });
            DropIndex("dbo.PurchaseItem", new[] { "PurchaseId" });
            DropIndex("dbo.Purchase", new[] { "ProcessedByUserId" });
            DropIndex("dbo.Purchase", new[] { "JobCardId" });
            DropIndex("dbo.Purchase", new[] { "CustomerId" });
            DropIndex("dbo.Delivery", new[] { "DriverUserId" });
            DropIndex("dbo.Delivery", new[] { "PurchaseId" });
            DropTable("dbo.UserChangeRequest");
            DropTable("dbo.DiagnosticReport");
            DropTable("dbo.Vehicle");
            DropTable("dbo.JobCard");
            DropTable("dbo.Product");
            DropTable("dbo.PurchaseItem");
            DropTable("dbo.Purchase");
            DropTable("dbo.User");
            DropTable("dbo.Delivery");
        }
    }
}
