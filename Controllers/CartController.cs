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


        // Add product to cart
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


        // Show cart
        [SessionAuthFilter]
        public ActionResult Index()
        {
            int userId = SessionHelper.GetUserId();

            var items = GetCartItems(userId);

            return View(items);
        }


        // Update cart quantity
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


        // Remove item from cart
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


        // Checkout page
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

            return View(items);
        }


        // Place order
        [HttpPost]
        [SessionAuthFilter]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(
            string fullName,
            string email,
            string phone,
            string address,
            string notes,
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

            // Get selected payment method
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

            decimal total = cartItems
                .Sum(c => c.Price * c.Quantity);

            // Store delivery information in SpecialRequest
            var requestParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address))
            {
                requestParts.Add(
                    "Delivery address: " + address.Trim());
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                requestParts.Add(
                    "Phone: " + phone.Trim());
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                requestParts.Add(
                    "Notes: " + notes.Trim());
            }

            // Create booking
            var booking = new Booking
            {
                UserId = userId,
                BookingDate = DateTime.Now,
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


            // Create booking details
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


            // Empty the cart after successful order
            _db.Carts.RemoveRange(cartItems);
            _db.SaveChanges();


            // Store information for Thank You page
            TempData["BookingId"] = booking.BookingId;
            TempData["TotalAmount"] = total;

            TempData["CustomerName"] =
                string.IsNullOrWhiteSpace(fullName)
                    ? "Valued Customer"
                    : fullName.Trim();

            TempData["PaymentMethod"] =
                paymentType.PaymentTypeName;

            return RedirectToAction("ThankYou");
        }


        // Get cart data as JSON
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

                // Get item name
                ProductName =
                    c.Product != null
                        ? c.Product.ProductName
                        : c.Package != null
                            ? c.Package.PackageName
                            : "Item",

                Quantity = c.Quantity,
                Price = c.Price,

                // Calculate item total
                LineTotal = c.Price * c.Quantity
            }).ToList();

            decimal subtotal = items
                .Sum(c => c.Price * c.Quantity);

            int cartCount = items
                .Sum(c => c.Quantity);

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


        // Thank You page
        public ActionResult ThankYou()
        {
            if (TempData["BookingId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BookingId = TempData["BookingId"];
            ViewBag.TotalAmount = TempData["TotalAmount"];
            ViewBag.CustomerName = TempData["CustomerName"];
            ViewBag.PaymentMethod =
                TempData["PaymentMethod"] ?? "Not specified";

            return View();
        }


        // Dispose database connection
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