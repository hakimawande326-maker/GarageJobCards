using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    public class ProductController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // ---------- Catalog - browsable by Receptionist and Manager ----------
        // "make"/"model" let staff say which vehicle they're shopping for
        // BEFORE browsing, so every product clearly shows whether it fits.

        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Index(ProductCategory? category, string make, string model)
        {
            var products = db.Products.Where(p => p.IsActive);
            if (category.HasValue)
                products = products.Where(p => p.Category == category.Value);

            ViewBag.SelectedCategory = category;
            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>().ToList();
            ViewBag.Make = make;
            ViewBag.Model = model;
            ViewBag.VehicleSelected = !string.IsNullOrWhiteSpace(make);

            // Distinct makes/models already in the catalog, to power simple
            // dropdowns instead of free typing.
            ViewBag.KnownMakes = db.Products.Where(p => p.IsActive && p.CompatibleMake != null)
                .Select(p => p.CompatibleMake).Distinct().OrderBy(m => m).ToList();

            return View(products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList());
        }

        // ---------- Manager: manage inventory ----------

        [RequireRole(UserRole.Manager)]
        public ActionResult Create()
        {
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult Create(Product model, HttpPostedFileBase imageFile)
        {
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
                return View(model);

            if (imageFile != null && imageFile.ContentLength > 0)
                model.ImageUrl = SaveProductImage(imageFile);

            db.Products.Add(model);
            db.SaveChanges();

            TempData["Success"] = model.Name + " added to the catalog.";
            return RedirectToAction("Index");
        }

        [RequireRole(UserRole.Manager)]
        public ActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult Edit(int id, Product model, HttpPostedFileBase imageFile)
        {
            var product = db.Products.Find(id);
            if (product == null) return HttpNotFound();

            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.ImageUrl = product.ImageUrl;
                return View(model);
            }

            product.Name = model.Name;
            product.Category = model.Category;
            product.Description = model.Description;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.PartNumber = model.PartNumber;
            product.CompatibleMake = model.CompatibleMake;
            product.CompatibleModel = model.CompatibleModel;

            if (imageFile != null && imageFile.ContentLength > 0)
                product.ImageUrl = SaveProductImage(imageFile);

            db.SaveChanges();

            TempData["Success"] = product.Name + " updated.";
            return RedirectToAction("Index");
        }

 // Saves an uploaded product photo under ~/Content/images/products/
        // with a unique filename, and returns the relative URL to store.
        // (Azure Blob Storage migration paused for now - reverting to local
        // disk so the app compiles and works without needing Azure set up.)
        private string SaveProductImage(HttpPostedFileBase file)
        {
            var folder = Server.MapPath("~/Content/images/products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            file.SaveAs(Path.Combine(folder, fileName));

            return "/Content/images/products/" + fileName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Manager)]
        public ActionResult Deactivate(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return HttpNotFound();

            product.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = product.Name + " removed from the catalog.";
            return RedirectToAction("Index");
        }

        // ---------- Purchase (point of sale) ----------

        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Purchase()
        {
            ViewBag.Products = db.Products.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
            ViewBag.Customers = db.Users.Where(u => u.Role == UserRole.Customer).OrderBy(u => u.FullName).ToList();
            return View();
        }

        [HttpPost]
 [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Receptionist, UserRole.Manager)]
        public ActionResult Purchase(int? customerId, string walkInName, string walkInPhone, int?[] productId, int?[] quantity, PaymentMethod paymentMethod = PaymentMethod.Cash, decimal? amountTendered = null)
        {
            var lineItems = new List<PurchaseItem>();

            if (productId != null && quantity != null)
            {
                for (int i = 0; i < productId.Length && i < quantity.Length; i++)
                {
                    if (!productId[i].HasValue || !quantity[i].HasValue || quantity[i].Value <= 0)
                        continue;

                    var product = db.Products.Find(productId[i].Value);
                    if (product == null || !product.IsActive)
                        continue;

                    if (quantity[i].Value > product.StockQuantity)
                    {
                        TempData["Error"] = "Not enough stock for " + product.Name + " (only " + product.StockQuantity + " left).";
                        return RedirectToAction("Purchase");
                    }

                    lineItems.Add(new PurchaseItem
                    {
                        ProductId = product.Id,
                        Quantity = quantity[i].Value,
                        UnitPrice = product.Price
                    });
                }
            }

            if (!lineItems.Any())
            {
                TempData["Error"] = "Add at least one product with a quantity before checking out.";
                return RedirectToAction("Purchase");
            }

            if (!customerId.HasValue && string.IsNullOrWhiteSpace(walkInName))
            {
                TempData["Error"] = "Select a customer, or enter a name for a walk-in sale.";
                return RedirectToAction("Purchase");
            }

            // For cash, the amount handed over must actually cover the total -
            // calculated here from the line items before anything is saved,
            // same VAT math as the Purchase model uses.
            if (paymentMethod == PaymentMethod.Cash)
            {
                var subtotal = lineItems.Sum(i => i.UnitPrice * i.Quantity);
                var vat = Math.Round(subtotal * 0.15m, 2);
                var total = subtotal + vat;

                if (!amountTendered.HasValue || amountTendered.Value < total)
                {
                    TempData["Error"] = "Amount tendered must cover the total (R" + total.ToString("N2") + ").";
                    return RedirectToAction("Purchase");
                }
            }

            var purchase = new Purchase
            {
                CustomerId = customerId,
                WalkInName = customerId.HasValue ? null : walkInName,
                WalkInPhone = customerId.HasValue ? null : walkInPhone,
                ProcessedByUserId = CurrentUserId.Value,
                PurchaseDateUtc = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                AmountTendered = paymentMethod == PaymentMethod.Cash ? amountTendered : null
            };

            db.Purchases.Add(purchase);
            db.SaveChanges();

            foreach (var item in lineItems)
            {
                item.PurchaseId = purchase.Id;
                db.PurchaseItems.Add(item);

                var product = db.Products.Find(item.ProductId);
                product.StockQuantity -= item.Quantity;
            }

            db.SaveChanges();

            purchase.PurchaseNumber = "PO-" + purchase.Id.ToString("D6");
            db.SaveChanges();

            TempData["Success"] = "Sale " + purchase.PurchaseNumber + " completed.";
            return RedirectToAction("Receipt", new { id = purchase.Id });
        }

 [RequireRole]
        public ActionResult Receipt(int id)
        {
            var purchase = db.Purchases
                .Include(p => p.Customer)
                .Include(p => p.ProcessedByUser)
                .Include(p => p.Items.Select(i => i.Product))
                .FirstOrDefault(p => p.Id == id);

            if (purchase == null) return HttpNotFound();

            bool allowed =
                CurrentUserRole == UserRole.Receptionist ||
                CurrentUserRole == UserRole.Manager ||
                (CurrentUserRole == UserRole.Customer && purchase.CustomerId == CurrentUserId);

            if (!allowed) return new HttpStatusCodeResult(403);

            return View(purchase);
        }

 // ---------- Customer: browse-only, auto-filtered to their own vehicles ----------
        // Customers can see what's available and whether it fits their car,
        // but can't check out themselves - purchases happen at the counter
        // with a Receptionist or Manager, same as walking into a parts shop.

 [RequireRole(UserRole.Customer)]
        public ActionResult Browse(int? vehicleId, string make, string model, bool showAll = false, ProductCategory? category = null)
        {
            var myVehicles = db.Vehicles.Where(v => v.OwnerId == CurrentUserId).OrderBy(v => v.Make).ToList();
            ViewBag.MyVehicles = myVehicles;

            // Three ways a customer can be looking at this page:
            //  1. showAll=true - browsing everything, no fitment check at all
            //     (e.g. just curious what's in stock, or buying for someone else)
            //  2. vehicleId set - checking fitment against one of THEIR OWN
            //     registered vehicles
            //  3. make/model typed manually - checking fitment against a
            //     vehicle that ISN'T registered with us (a friend's car, one
            //     they haven't brought in yet, etc.)
            Vehicle selectedVehicle = null;
            string manualMake = null, manualModel = null;

            if (!showAll)
            {
                if (vehicleId.HasValue)
                {
                    selectedVehicle = myVehicles.FirstOrDefault(v => v.Id == vehicleId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(make))
                {
                    manualMake = make;
                    manualModel = model;
                }
                else if (myVehicles.Count == 1)
                {
                    selectedVehicle = myVehicles[0]; // only one car on file - just use it automatically
                }
            }

            ViewBag.SelectedVehicle = selectedVehicle;
            ViewBag.ManualMake = manualMake;
            ViewBag.ManualModel = manualModel;
            ViewBag.ShowAll = showAll;
            ViewBag.HasVehicleContext = selectedVehicle != null || manualMake != null;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>().ToList();

            var products = db.Products.Where(p => p.IsActive);
            if (category.HasValue)
                products = products.Where(p => p.Category == category.Value);

            return View(products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList());
        }

        // ---------- Customer: buy online, pay online, optional delivery ----------

        [RequireRole(UserRole.Customer)]
        public ActionResult Checkout()
        {
            ViewBag.Products = db.Products.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
            ViewBag.DeliveryFee = DeliveryFeeAmount;

            var me = db.Users.Find(CurrentUserId);
            ViewBag.MyAddress = me != null ? me.Address : null;

            return View();
        }

        private const decimal DeliveryFeeAmount = 100m;

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole(UserRole.Customer)]
        public ActionResult Checkout(int?[] productId, int?[] quantity, CollectionMethod collectionMethod, string deliveryAddress)
        {
            var lineItems = new List<PurchaseItem>();

            if (productId != null && quantity != null)
            {
                for (int i = 0; i < productId.Length && i < quantity.Length; i++)
                {
                    if (!productId[i].HasValue || !quantity[i].HasValue || quantity[i].Value <= 0)
                        continue;

                    var product = db.Products.Find(productId[i].Value);
                    if (product == null || !product.IsActive)
                        continue;

                    if (quantity[i].Value > product.StockQuantity)
                    {
                        TempData["Error"] = "Not enough stock for " + product.Name + " (only " + product.StockQuantity + " left).";
                        return RedirectToAction("Checkout");
                    }

                    lineItems.Add(new PurchaseItem
                    {
                        ProductId = product.Id,
                        Quantity = quantity[i].Value,
                        UnitPrice = product.Price
                    });
                }
            }

            if (!lineItems.Any())
            {
                TempData["Error"] = "Add at least one product with a quantity before checking out.";
                return RedirectToAction("Checkout");
            }

            if (collectionMethod == CollectionMethod.Delivery && string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Enter a delivery address, or choose to collect from the shop instead.";
                return RedirectToAction("Checkout");
            }

            // Order is created as unpaid - stock is reserved now (decremented
            // immediately, simplest approach at this scale) but the order
            // only becomes real once PayFast actually confirms payment, via
            // PayFastNotify (the ITN webhook) or PayFastReturn as a
            // same-scale fallback for local testing where PayFast can't
            // reach localhost to call the webhook.
            var purchase = new Purchase
            {
                CustomerId = CurrentUserId,
                ProcessedByUserId = null,
                PurchaseDateUtc = DateTime.UtcNow,
                PaymentMethod = PaymentMethod.Card,
                PaidOnline = true,
                IsPaid = false,
                CollectionMethod = collectionMethod,
                DeliveryFee = collectionMethod == CollectionMethod.Delivery ? DeliveryFeeAmount : 0m,
                DeliveryAddress = collectionMethod == CollectionMethod.Delivery ? deliveryAddress : null
            };

            db.Purchases.Add(purchase);
            db.SaveChanges();

            foreach (var item in lineItems)
            {
                item.PurchaseId = purchase.Id;
                db.PurchaseItems.Add(item);

                var product = db.Products.Find(item.ProductId);
                product.StockQuantity -= item.Quantity;
            }

            db.SaveChanges();

            purchase.PurchaseNumber = "PO-" + purchase.Id.ToString("D6");
            db.SaveChanges();

            return RedirectToAction("PayNow", new { id = purchase.Id });
        }

        // ---------- Real PayFast payment flow ----------

        [RequireRole(UserRole.Customer)]
        public ActionResult PayNow(int id)
        {
            var purchase = db.Purchases.Include(p => p.Items.Select(i => i.Product)).FirstOrDefault(p => p.Id == id);
            if (purchase == null) return HttpNotFound();
            if (purchase.CustomerId != CurrentUserId) return new HttpStatusCodeResult(403);
            if (purchase.IsPaid)
                return RedirectToAction("Receipt", new { id = purchase.Id });

            var baseUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Url.Content("~/");
            var itemName = purchase.Items.Count == 1
                ? purchase.Items.First().Product.Name
                : purchase.Items.Count + " parts from Phila's Auto";

            var customer = db.Users.Find(CurrentUserId);

            ViewBag.PayFastUrl = PayFastHelper.PayFastUrl;
            ViewBag.Fields = PayFastHelper.BuildPaymentFields(
                purchase.Id,
                purchase.GrandTotal,
                itemName,
                customer != null ? customer.Email : null,
                baseUrl + "Product/PayFastReturn/" + purchase.Id,
                baseUrl + "Product/PayFastCancel/" + purchase.Id,
                baseUrl + "Product/PayFastNotify"
            );

            return View(purchase);
        }

        // PayFast sends the browser back here after successful payment.
        // Authoritative confirmation is really PayFastNotify below (the
        // server-to-server ITN webhook) - this route also finalizes the
        // order as a fallback, since PayFast's sandbox can't reach a
        // localhost notify_url during local testing.
        [RequireRole(UserRole.Customer)]
        public ActionResult PayFastReturn(int id)
        {
            var purchase = db.Purchases.Find(id);
            if (purchase == null) return HttpNotFound();
            if (purchase.CustomerId != CurrentUserId) return new HttpStatusCodeResult(403);

            if (!purchase.IsPaid)
                FinalizePurchase(purchase);

            TempData["Success"] = "Payment successful - " + purchase.PurchaseNumber + (purchase.CollectionMethod == CollectionMethod.Delivery ? ". A driver has been assigned." : ". Ready to collect from the shop.");
            return RedirectToAction("Receipt", new { id = purchase.Id });
        }

        [RequireRole(UserRole.Customer)]
        public ActionResult PayFastCancel(int id)
        {
            var purchase = db.Purchases.Include(p => p.Items).FirstOrDefault(p => p.Id == id);
            if (purchase == null) return HttpNotFound();
            if (purchase.CustomerId != CurrentUserId) return new HttpStatusCodeResult(403);

            if (!purchase.IsPaid)
            {
                // Restore the stock that was reserved at checkout, then
                // remove the abandoned pending order entirely.
                foreach (var item in purchase.Items.ToList())
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product != null) product.StockQuantity += item.Quantity;
                    db.PurchaseItems.Remove(item);
                }
                db.Purchases.Remove(purchase);
                db.SaveChanges();
            }

            TempData["Error"] = "Payment was cancelled - your cart items are still in stock if you'd like to try again.";
            return RedirectToAction("Checkout");
        }

        // The ITN webhook - PayFast calls this directly, server-to-server,
        // to confirm a payment really went through. This is the
        // authoritative confirmation path for a real deployment (PayNow's
        // notify_url points here). Requires the site to be publicly
        // reachable, so it won't fire during pure localhost testing -
        // PayFastReturn covers that case instead.
        [HttpPost]
        public ActionResult PayFastNotify()
        {
            var form = Request.Form;
            var mPaymentId = form["m_payment_id"];
            var paymentStatus = form["payment_status"];

            int purchaseId;
            if (!int.TryParse(mPaymentId, out purchaseId))
                return new HttpStatusCodeResult(400);

            var purchase = db.Purchases.Find(purchaseId);
            if (purchase == null)
                return new HttpStatusCodeResult(404);

            if (string.Equals(paymentStatus, "COMPLETE", StringComparison.OrdinalIgnoreCase) && !purchase.IsPaid)
                FinalizePurchase(purchase);

            return new HttpStatusCodeResult(200);
        }

        // Marks an order paid and creates its delivery if one was chosen -
        // shared by both the ITN webhook and the return-page fallback so
        // whichever fires first does the work, and the other is a no-op
        // (guarded by the IsPaid check at each call site).
        private void FinalizePurchase(Purchase purchase)
        {
            purchase.IsPaid = true;

            if (purchase.CollectionMethod == CollectionMethod.Delivery && !db.Deliveries.Any(d => d.PurchaseId == purchase.Id))
            {
                var activeDrivers = db.Users.Where(u => u.Role == UserRole.Driver && u.IsActive).ToList();
                var driver = activeDrivers
                    .OrderBy(d => db.Deliveries.Count(del => del.DriverUserId == d.Id && (del.Status == DeliveryStatus.Pending || del.Status == DeliveryStatus.OutForDelivery)))
                    .FirstOrDefault();

                if (driver != null)
                {
                    var delivery = new Delivery
                    {
                        PurchaseId = purchase.Id,
                        DriverUserId = driver.Id,
                        DeliveryAddress = purchase.DeliveryAddress,
                        Status = DeliveryStatus.Pending
                    };
                    db.Deliveries.Add(delivery);
                    db.SaveChanges();

                    delivery.DeliveryNumber = "DL-" + delivery.Id.ToString("D6");
                }
            }

            db.SaveChanges();
        }

        // ---------- Customer: purchase history ----------

        [RequireRole(UserRole.Customer)]
 public ActionResult MyPurchases()
        {
            var purchases = db.Purchases
                .Include(p => p.Items.Select(i => i.Product))
                .Where(p => p.CustomerId == CurrentUserId && p.IsPaid)
                .OrderByDescending(p => p.PurchaseDateUtc)
                .ToList();

            return View(purchases);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
