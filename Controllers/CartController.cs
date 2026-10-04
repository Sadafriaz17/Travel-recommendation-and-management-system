using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;
using TourwebsiteFYP.Helpers;

namespace TourwebsiteFYP.Controllers
{
    public class CartController : Controller
    {
        private Demo_DevDBEntities _db = new Demo_DevDBEntities();

        // Get all cart items for the logged-in user
        private List<Cart> GetCartItems(int userId)
        {
            var cartItems = _db.Carts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedDate)
                .ToList();

            if (!cartItems.Any())
                return cartItems;

            // Load products
            var productIds = cartItems
                .Where(c => c.ProductId.HasValue)
                .Select(c => c.ProductId.Value)
                .Distinct()
                .ToList();

            if (productIds.Any())
            {
                var products = _db.Products
                    .Include("ProductType")
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToList();

                foreach (var item in cartItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        item.Product = products
                            .FirstOrDefault(p => p.ProductId == item.ProductId.Value);
                    }
                }
            }

            // Load packages
            var packageIds = cartItems
                .Where(c => c.PackageId.HasValue)
                .Select(c => c.PackageId.Value)
                .Distinct()
                .ToList();

            if (packageIds.Any())
            {
                var packages = _db.Packages
                    .Include("PackageType")
                    .Include("PackageImages")
                    .Where(p => packageIds.Contains(p.PackageId))
                    .ToList();

                foreach (var item in cartItems)
                {
                    if (item.PackageId.HasValue)
                    {
                        item.Package = packages
                            .FirstOrDefault(p => p.PackageId == item.PackageId.Value);
                    }
                }
            }

            return cartItems;
        }


        // ─── ADD PRODUCT TO CART ────────────────────────────────────────────────
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            int userId = SessionHelper.GetUserId();

            if (userId == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Please log in.",
                    redirect = "/Login/Index"
                });
            }

            if (quantity < 1)
                quantity = 1;

            var product = _db.Products
                .FirstOrDefault(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Product not found."
                });
            }

            // Check if product is already in cart
            var existing = _db.Carts
                .FirstOrDefault(c =>
                    c.UserId == userId &&
                    c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.Price = product.Price;
            }
            else
            {
                var cartItem = new Cart
                {
                    UserId = userId,
                    ProductId = productId,
                    PackageId = null,
                    Quantity = quantity,
                    Price = product.Price,
                    AddedDate = DateTime.Now
                };

                _db.Carts.Add(cartItem);
            }

            _db.SaveChanges();

            int cartCount = _db.Carts
                .Where(c => c.UserId == userId)
                .Sum(c => (int?)c.Quantity) ?? 0;

            return Json(new
            {
                success = true,
                message = "Added to cart!",
                cartCount = cartCount
            });
        }


        // ─── ADD PACKAGE TO CART ────────────────────────────────────────────────
        // Called via AJAX from package listing / details buttons and also invoked
        // internally by BookingController.Checkout before the redirect.
        [HttpPost]
        public ActionResult AddPackageToCart(int packageId, int quantity = 1)
        {
            int userId = SessionHelper.GetUserId();

            if (userId == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Please log in.",
                    redirect = "/Login/Index"
                });
            }

            if (quantity < 1)
                quantity = 1;

            var package = _db.Packages
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Package not found."
                });
            }

            // Check if the package is already in the cart
            var existing = _db.Carts
                .FirstOrDefault(c =>
                    c.UserId == userId &&
                    c.PackageId == packageId);

            if (existing != null)
            {
                // Already present — just refresh the price in case it changed
                existing.Price = package.Price;
            }
            else
            {
                var cartItem = new Cart
                {
                    UserId = userId,
                    PackageId = packageId,
                    ProductId = null,
                    Quantity = quantity,
                    Price = package.Price,
                    AddedDate = DateTime.Now
                };

                _db.Carts.Add(cartItem);
            }

            _db.SaveChanges();

            int cartCount = _db.Carts
                .Where(c => c.UserId == userId)
                .Sum(c => (int?)c.Quantity) ?? 0;

            return Json(new
            {
                success = true,
                message = "Package added to cart!",
                cartCount = cartCount
            });
        }


        // ─── SHOW CART ──────────────────────────────────────────────────────────
        [SessionAuthFilter]
        public ActionResult Index()
        {
            int userId = SessionHelper.GetUserId();

            var items = GetCartItems(userId);

            return View(items);
        }


        // ─── UPDATE QUANTITY ────────────────────────────────────────────────────
        [HttpPost]
        [SessionAuthFilter]
        public ActionResult UpdateQuantity(int cartId, int quantity)
        {
            int userId = SessionHelper.GetUserId();

            var item = _db.Carts.FirstOrDefault(c =>
                c.CartId == cartId &&
                c.UserId == userId);

            if (item == null)
            {
                return Json(new
                {
                    success = false
                });
            }

            if (quantity <= 0)
            {
                _db.Carts.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            _db.SaveChanges();

            var remainingItems = _db.Carts
                .Where(c => c.UserId == userId)
                .ToList();

            decimal subtotal = remainingItems
                .Sum(c => c.Price * c.Quantity);

            int cartCount = remainingItems
                .Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                subtotal = subtotal.ToString("N2"),
                cartCount = cartCount
            });
        }


        // ─── REMOVE ITEM FROM CART ──────────────────────────────────────────────
        [HttpPost]
        [SessionAuthFilter]
        public ActionResult Remove(int cartId)
        {
            int userId = SessionHelper.GetUserId();

            var item = _db.Carts.FirstOrDefault(c =>
                c.CartId == cartId &&
                c.UserId == userId);

            if (item != null)
            {
                _db.Carts.Remove(item);
                _db.SaveChanges();
            }

            var remainingItems = _db.Carts
                .Where(c => c.UserId == userId)
                .ToList();

            decimal subtotal = remainingItems
                .Sum(c => c.Price * c.Quantity);

            int cartCount = remainingItems
                .Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                subtotal = subtotal.ToString("N2"),
                cartCount = cartCount
            });
        }


        // ─── CHECKOUT PAGE ──────────────────────────────────────────────────────
        // Shared for product orders, package bookings, and mixed carts.
        // ViewBag.HasPackages / ViewBag.HasProducts tell the view which
        // conditional fields to render.
        [SessionAuthFilter]
        public ActionResult Checkout()
        {
            int userId = SessionHelper.GetUserId();

            var items = GetCartItems(userId);

            if (!items.Any())
            {
                return RedirectToAction("Index");
            }

            ViewBag.User = _db.Users
                .FirstOrDefault(u => u.UserId == userId);

            ViewBag.PaymentTypes = _db.PaymentTypes
                .OrderBy(p => p.PaymentTypeId)
                .ToList();

            // Flags used in Checkout.cshtml to show / hide package-specific fields
            ViewBag.HasPackages = items.Any(c => c.PackageId.HasValue);
            ViewBag.HasProducts = items.Any(c => c.ProductId.HasValue);

            return View(items);
        }


        // ─── PLACE ORDER ────────────────────────────────────────────────────────
        // Handles product orders, package bookings, and mixed carts through a
        // single action.  travelDate and travelers are only submitted when the
        // cart contains at least one package.
        [HttpPost]
        [SessionAuthFilter]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(
            string fullName,
            string email,
            string phone,
            string address,
            string notes,
            string travelDate,
            int? travelers,
            int? paymentTypeId = null)
        {
            int userId = SessionHelper.GetUserId();

            if (userId == 0)
            {
                return RedirectToAction("Index", "Login");
            }

            var cartItems = _db.Carts
                .Where(c => c.UserId == userId)
                .ToList();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index");
            }

            // Validate payment method
            var paymentType = paymentTypeId.HasValue
                ? _db.PaymentTypes.FirstOrDefault(
                    p => p.PaymentTypeId == paymentTypeId.Value)
                : null;

            if (paymentType == null)
            {
                TempData["CheckoutError"] =
                    "Please choose a payment method before placing your order.";

                return RedirectToAction("Checkout");
            }

            bool hasPackages = cartItems.Any(c => c.PackageId.HasValue);
            bool hasProducts = cartItems.Any(c => c.ProductId.HasValue);

            decimal total = cartItems
                .Sum(c => c.Price * c.Quantity);

            // Build SpecialRequest from all submitted fields
            var requestParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address))
                requestParts.Add("Delivery address: " + address.Trim());

            if (!string.IsNullOrWhiteSpace(phone))
                requestParts.Add("Phone: " + phone.Trim());

            // Package-specific fields stored alongside the rest
            if (hasPackages)
            {
                if (!string.IsNullOrWhiteSpace(travelDate))
                    requestParts.Add("Travel date: " + travelDate.Trim());

                if (travelers.HasValue && travelers.Value > 0)
                    requestParts.Add("Travellers: " + travelers.Value);
            }

            if (!string.IsNullOrWhiteSpace(notes))
                requestParts.Add("Notes: " + notes.Trim());

            // Resolve BookingDate: use the chosen travel date for package bookings;
            // fall back to now for pure product orders.
            DateTime bookingDate = DateTime.Now;
            if (hasPackages &&
                !string.IsNullOrWhiteSpace(travelDate) &&
                DateTime.TryParse(travelDate, out DateTime parsedDate) &&
                parsedDate.Date >= DateTime.Today)
            {
                bookingDate = parsedDate;
            }

            // Create booking record (one per checkout, regardless of item types)
            var booking = new Booking
            {
                UserId = userId,
                BookingDate = bookingDate,
                Status = "Pending",
                TotalAmount = total,
                Email = email,
                SpecialRequest = requestParts.Any()
                    ? string.Join(" | ", requestParts)
                    : null,
                CreatedDate = DateTime.Now
            };

            _db.Bookings.Add(booking);
            _db.SaveChanges();


            // Create booking details — one row per cart item.
            // BookingDetail already has nullable ProductId and PackageId columns,
            // so no schema change is required.
            foreach (var item in cartItems)
            {
                var bookingDetail = new BookingDetail
                {
                    BookingId = booking.BookingId,
                    PackageId = item.PackageId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                _db.BookingDetails.Add(bookingDetail);
            }

            _db.SaveChanges();


            // Create payment record
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaymentDate = DateTime.Now,
                Amount = total,
                PaymentTypeId = paymentType.PaymentTypeId,
                PaymentMethod = paymentType.PaymentTypeName,
                Status = "Pending"
            };

            _db.Payments.Add(payment);
            _db.SaveChanges();


            // Clear the cart
            _db.Carts.RemoveRange(cartItems);
            _db.SaveChanges();


            // Pass data to the shared Thank You page
            TempData["BookingId"]     = booking.BookingId;
            TempData["TotalAmount"]   = total;
            TempData["CustomerName"]  = string.IsNullOrWhiteSpace(fullName)
                                            ? "Valued Customer"
                                            : fullName.Trim();
            TempData["PaymentMethod"] = paymentType.PaymentTypeName;

            // OrderType drives the messaging on the Thank You page
            if (hasPackages && hasProducts)
                TempData["OrderType"] = "mixed";
            else if (hasPackages)
                TempData["OrderType"] = "packages";
            else
                TempData["OrderType"] = "products";

            // Package-specific summary data for the Thank You page
            if (hasPackages)
            {
                TempData["TravelDate"] = travelDate;
                TempData["Travelers"]  = travelers.HasValue ? travelers.Value : 1;
            }

            return RedirectToAction("ThankYou");
        }


        // ─── GET CART DATA (JSON) ───────────────────────────────────────────────
        [HttpGet]
        public ActionResult GetCartData()
        {
            if (Session["UserId"] == null)
            {
                return Json(
                    new { success = false },
                    JsonRequestBehavior.AllowGet);
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var items = GetCartItems(userId);

            var cartData = items.Select(c => new
            {
                CartId = c.CartId,
                ProductId = c.ProductId,
                PackageId = c.PackageId,

                ProductName =
                    c.Product != null
                        ? c.Product.ProductName
                        : c.Package != null
                            ? c.Package.PackageName
                            : "Item",

                Quantity = c.Quantity,
                Price = c.Price,
                LineTotal = c.Price * c.Quantity
            }).ToList();

            decimal subtotal = items.Sum(c => c.Price * c.Quantity);
            int cartCount    = items.Sum(c => c.Quantity);

            return Json(
                new
                {
                    success = true,
                    items = cartData,
                    subtotal = subtotal,
                    cartCount = cartCount
                },
                JsonRequestBehavior.AllowGet);
        }


        // ─── THANK YOU PAGE ─────────────────────────────────────────────────────
        // Shared for all order types: products, packages, or mixed.
        public ActionResult ThankYou()
        {
            if (TempData["BookingId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BookingId     = TempData["BookingId"];
            ViewBag.TotalAmount   = TempData["TotalAmount"];
            ViewBag.CustomerName  = TempData["CustomerName"];
            ViewBag.PaymentMethod = TempData["PaymentMethod"] ?? "Not specified";
            ViewBag.OrderType     = TempData["OrderType"]     ?? "products";
            ViewBag.TravelDate    = TempData["TravelDate"] as string;
            ViewBag.Travelers     = TempData["Travelers"];

            return View();
        }


        // ─── DISPOSE ────────────────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}