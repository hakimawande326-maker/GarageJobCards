using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public enum ProductCategory
    {
        BumpersAndGrills = 0,
        BrakesAndClutch = 1,
        Radiators = 2,
        AirconsCondensersAndFans = 3,
        SuspensionAndWheelBearings = 4,
        IgnitionAndDoorLocks = 5,
        DoorMirrors = 6,
        FendersDoorsAndBonnets = 7,
        Lights = 8
    }

    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        public ProductCategory Category { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 500000, ErrorMessage = "Price must be between R0.01 and R500,000.")]
        [Display(Name = "Price (R, excl. VAT)")]
        public decimal Price { get; set; }

        [Range(0, 100000, ErrorMessage = "Stock must be between 0 and 100,000.")]
        [Display(Name = "Stock on hand")]
        public int StockQuantity { get; set; }

        // Optional part number for staff's own reference / supplier lookup.
        [StringLength(40)]
        [Display(Name = "Part number (optional)")]
        public string PartNumber { get; set; }

        // Relative path under ~/Content/images/products/ - null means no
        // photo uploaded yet, catalog shows a placeholder instead.
        public string ImageUrl { get; set; }

        // Which vehicle this part fits. Leave both blank for a universal
        // part (fits everything) - e.g. generic brake fluid.
        [StringLength(40)]
        [Display(Name = "Compatible Make (leave blank if universal)")]
        public string CompatibleMake { get; set; }

        [StringLength(40)]
        [Display(Name = "Compatible Model (leave blank if fits all models of that make)")]
        public string CompatibleModel { get; set; }

        public bool IsActive { get; set; }

        public Product()
        {
            IsActive = true;
        }

        // True if this product fits the given vehicle, or is universal.
        public bool FitsVehicle(string make, string model)
        {
            if (string.IsNullOrWhiteSpace(CompatibleMake)) return true; // universal part

            if (string.IsNullOrWhiteSpace(make)) return false;
            if (!CompatibleMake.Trim().Equals(make.Trim(), System.StringComparison.OrdinalIgnoreCase)) return false;

            if (string.IsNullOrWhiteSpace(CompatibleModel)) return true; // fits any model of that make

            if (string.IsNullOrWhiteSpace(model)) return false;
            return CompatibleModel.Trim().Equals(model.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }

        public static string CategoryLabel(ProductCategory category)
        {
            switch (category)
            {
                case ProductCategory.BumpersAndGrills: return "Bumpers & Grills";
                case ProductCategory.BrakesAndClutch: return "Brakes & Clutch";
                case ProductCategory.Radiators: return "Radiators";
                case ProductCategory.AirconsCondensersAndFans: return "Aircons, Condensers & Fans";
                case ProductCategory.SuspensionAndWheelBearings: return "Suspension & Wheel Bearings";
                case ProductCategory.IgnitionAndDoorLocks: return "Ignition & Door Locks";
                case ProductCategory.DoorMirrors: return "Door Mirrors";
                case ProductCategory.FendersDoorsAndBonnets: return "Fenders, Doors & Bonnets";
                case ProductCategory.Lights: return "Lights";
                default: return category.ToString();
            }
        }
    }
}
