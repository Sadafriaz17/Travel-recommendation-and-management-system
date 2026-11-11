using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TourwebsiteFYP.DB_data_models;
using TourwebsiteFYP.Filter;
using TourwebsiteFYP.Helpers;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class SafepayController : Controller
    {
        // ─── Non-terminal tracker states ──────────────────────────────────────
        // These states mean the payment is still in progress — do NOT mark as Failed.
        private static readonly string[] NonTerminalStates =
        {
            "TRACKER_STARTED",
            "TRACKER_AUTHORIZED",
            "TRACKER_ENROLLED"
        };

        // Terminal success state
        private const string StateEnded    = "TRACKER_ENDED";

        // Terminal cancellation / expiry states
        private const string StateCancelled = "TRACKER_CANCELLED";
        private const string StateExpired   = "TRACKER_EXPIRED";


        // ─── Helper: clean the tracker value coming back from Safepay ─────────
        /// <summary>
        /// Our callback URLs already carry a tracker value, and Safepay also adds one
        /// when it sends the customer back. That can reach us as two joined values
        /// (for example "track_a,track_a") or as a value with extra text after it.
        /// This returns only the first real tracker token so the database lookup works.
        /// </summary>
        private static string NormalizeTracker(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            var m = Regex.Match(raw, @"track_[A-Za-z0-9\-]+");
            return m.Success ? m.Value : raw.Trim();
        }


        // ─── Helper: cart-clearing (shared by PaymentCallback and Webhook) ────

        /// <summary>
        /// Removes only the quantities that were ordered (captured in BookingDetails)
        /// from the user's cart. Extra quantity above what was ordered is left intact.
        /// Must be called inside an open DbContext; changes are committed by the caller.
        /// </summary>
        private static void ClearOrderedCartItems(Demo_DevDBEntities db, int bookingId, int userId)
        {
            var details = db.BookingDetails
                            .Where(d => d.BookingId == bookingId)
                            .ToList();

            foreach (var detail in details)
            {
                Cart cartItem = null;

                if (detail.ProductId.HasValue)
                    cartItem = db.Carts.FirstOrDefault(c =>
                        c.UserId == userId && c.ProductId == detail.ProductId.Value);
                else if (detail.PackageId.HasValue)
                    cartItem = db.Carts.FirstOrDefault(c =>
                        c.UserId == userId && c.PackageId == detail.PackageId.Value);

                if (cartItem != null)
                {
                    int orderedQty = detail.Quantity ?? 1;
                    if (cartItem.Quantity <= orderedQty)
                        db.Carts.Remove(cartItem);
                    else
                        cartItem.Quantity -= orderedQty;
                }
            }

            db.SaveChanges();
        }


        // ─── 1. InitiatePayment (GET) ─────────────────────────────────────────

        /// <summary>
        /// Creates a Safepay tracker, obtains a passport (TBT) token and redirects
        /// the browser to the Safepay hosted checkout page.
        ///
        /// Route: GET /Safepay/InitiatePayment?paymentId=X
        /// </summary>
        [SessionAuthFilter]
        [HttpGet]
        public ActionResult InitiatePayment(int paymentId)
        {
            int userId = SessionHelper.GetUserId();

            using (var db = new Demo_DevDBEntities())
            {
                // ── Verify ownership and status ─────────────────────────────────
                var payment = db.Payments
                                .Include(p => p.Booking)
                                .FirstOrDefault(p => p.PaymentId == paymentId);

                if (payment == null
                    || payment.Booking == null
                    || payment.Booking.UserId != userId)
                {
                    TempData["CheckoutError"] = "Payment record not found or access denied.";
                    return RedirectToAction("Checkout", "Cart");
                }

                if (!string.Equals(payment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["CheckoutError"] = "This payment has already been processed.";
                    return RedirectToAction("Checkout", "Cart");
                }

                int     bookingId = payment.BookingId;
                decimal amount    = payment.Amount;   // decimal (non-nullable in EF entity)

                // ── Step 1: Create tracker ──────────────────────────────────────
                var trackerResult = SafepayService.CreateTracker(amount, "NEXVOY-" + paymentId);
                if (!trackerResult.Success)
                {
                    // DIAGNOSTIC: surface actual error — REMOVE before production
                    TempData["CheckoutError"] =
                        "Could not initiate card payment. [DIAG: " + trackerResult.ErrorMessage + "]";
                    return RedirectToAction("Checkout", "Cart");
                }

                string trackerToken = trackerResult.TrackerToken;

                // ── Step 2: Get passport (TBT) token ────────────────────────────
                var tbtResult = SafepayService.GetPassportToken();
                if (!tbtResult.Success)
                {
                    TempData["CheckoutError"] =
                        "Could not authorise payment session. Please try again.";
                    return RedirectToAction("Checkout", "Cart");
                }

                // ── Step 3: Persist attempt record and stamp tracker on Payment ──
                SafepayDbHelper.InsertAttempt(paymentId, bookingId, trackerToken, amount);
                SafepayDbHelper.UpdatePaymentSafepayTracker(paymentId, trackerToken);

                // ── Step 4: Build callback URLs ─────────────────────────────────
                string redirectUrl = Url.Action(
                    "PaymentCallback", "Safepay",
                    new { paymentId = paymentId, tracker = trackerToken },
                    Request.Url.Scheme);

                string cancelUrl = Url.Action(
                    "PaymentCancelled", "Safepay",
                    new { paymentId = paymentId, tracker = trackerToken },
                    Request.Url.Scheme);

                // ── Step 5: Build checkout URL and redirect ──────────────────────
                string checkoutUrl = SafepayService.BuildCheckoutUrl(
                    trackerToken,
                    tbtResult.TbtToken,
                    redirectUrl,
                    cancelUrl);

                return Redirect(checkoutUrl);
            }
        }


        // ─── 2. PaymentCallback (GET) ─────────────────────────────────────────

        /// <summary>
        /// Safepay redirects here after successful payment completion.
        ///
        /// Route: GET /Safepay/PaymentCallback?paymentId=X&amp;tracker=Y
        ///
        /// Verification steps:
        ///   1. Verify payment ownership (Booking.UserId == logged-in user)
        ///   2. Load local SafepayPaymentAttempt and confirm tracker matches paymentId
        ///   3. Call GET /reporter/api/v1/payments/{tracker} server-side
        ///   4. Verify state == "TRACKER_ENDED"
        ///   5. Verify returned amount matches local Payment.Amount (in paisas)
        ///   6. Verify returned currency == "PKR"
        ///   7. Update Payment and Booking in a DB transaction
        /// </summary>
        [SessionAuthFilter]
        [HttpGet]
        public ActionResult PaymentCallback(int paymentId, string tracker)
        {
            int userId = SessionHelper.GetUserId();

            // Safepay may add a second tracker value to the return URL. Keep only the first token.
            tracker = NormalizeTracker(tracker);

            if (string.IsNullOrWhiteSpace(tracker))
            {
                TempData["CheckoutError"] = "Invalid payment callback. Please contact support.";
                return RedirectToAction("Checkout", "Cart");
            }

            using (var db = new Demo_DevDBEntities())
            {
                // ── Verify ownership ────────────────────────────────────────────
                var payment = db.Payments
                                .Include(p => p.Booking)
                                .FirstOrDefault(p => p.PaymentId == paymentId);

                if (payment == null
                    || payment.Booking == null
                    || payment.Booking.UserId != userId)
                {
                    TempData["CheckoutError"] = "Payment record not found or access denied.";
                    return RedirectToAction("Checkout", "Cart");
                }

                int bookingId = payment.BookingId;

                // ── Verify tracker ownership against local attempt ──────────────
                var attempt = SafepayDbHelper.GetAttemptByToken(tracker);
                if (attempt == null)
                {
                    TempData["CheckoutError"] = "Payment attempt record not found. Please contact support.";
                    return RedirectToAction("Checkout", "Cart");
                }

                if (attempt.PaymentId != paymentId || attempt.BookingId != bookingId)
                {
                    TempData["CheckoutError"] = "Payment verification failed. Please contact support.";
                    return RedirectToAction("Checkout", "Cart");
                }

                // ── Idempotency guard ───────────────────────────────────────────
                if (string.Equals(attempt.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                {
                    SetThankYouTempData(payment, bookingId, db);
                    return RedirectToAction("ThankYou", "Cart");
                }

                // ── Server-side tracker verification ───────────────────────────
                var verifyResult = SafepayService.VerifyTracker(tracker);

                if (!verifyResult.Success)
                {
                    // DIAGNOSTIC: shows the reason. REMOVE the DIAG part before production.
                    TempData["CheckoutError"] =
                        "Could not verify payment status. Please wait a moment and check your booking history, or contact support. [DIAG: "
                        + verifyResult.ErrorMessage + "]";
                    return RedirectToAction("Checkout", "Cart");
                }

                // ── Check for non-terminal states (payment still in progress) ───
                if (!string.IsNullOrWhiteSpace(verifyResult.State))
                {
                    foreach (var nonTerminal in NonTerminalStates)
                    {
                        if (string.Equals(verifyResult.State, nonTerminal, StringComparison.OrdinalIgnoreCase))
                        {
                            TempData["CheckoutError"] =
                                "Your payment is still being processed. Please wait a moment and check your booking history.";
                            return RedirectToAction("Checkout", "Cart");
                        }
                    }
                }

                bool isPaid = string.Equals(verifyResult.State, StateEnded, StringComparison.OrdinalIgnoreCase);

                if (isPaid)
                {
                    // ── Amount/currency cross-check ─────────────────────────────
                    long expectedPaisas = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);

                    if (verifyResult.Amount > 0 && verifyResult.Amount != expectedPaisas)
                    {
                        TempData["CheckoutError"] =
                            "Payment amount mismatch. Please contact support with your booking reference.";
                        return RedirectToAction("Checkout", "Cart");
                    }

                    if (!string.IsNullOrWhiteSpace(verifyResult.Currency)
                        && !string.Equals(verifyResult.Currency, "PKR", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["CheckoutError"] =
                            "Payment currency mismatch. Please contact support with your booking reference.";
                        return RedirectToAction("Checkout", "Cart");
                    }

                    // ── Confirm payment in a DB transaction ─────────────────────
                    using (var tx = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (!string.Equals(payment.Status, "Completed",
                                               StringComparison.OrdinalIgnoreCase))
                                payment.Status = "Completed";

                            var booking = payment.Booking;
                            if (!string.Equals(booking.Status, "Confirmed",
                                               StringComparison.OrdinalIgnoreCase))
                                booking.Status = "Confirmed";

                            db.SaveChanges();

                            ClearOrderedCartItems(db, bookingId, userId);

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    SafepayDbHelper.UpdateAttemptStatus(tracker, "Paid");
                    SetThankYouTempData(payment, bookingId, db);
                    return RedirectToAction("ThankYou", "Cart");
                }
                else
                {
                    // ── Terminal failure states ─────────────────────────────────
                    bool isCancelled = string.Equals(verifyResult.State, StateCancelled,
                                                     StringComparison.OrdinalIgnoreCase);
                    bool isExpired   = string.Equals(verifyResult.State, StateExpired,
                                                     StringComparison.OrdinalIgnoreCase);

                    if (isCancelled)
                    {
                        SafepayDbHelper.UpdateAttemptStatus(tracker, "Cancelled");
                        TempData["CheckoutError"] =
                            "Payment was cancelled. Your cart is preserved — you can try again.";
                    }
                    else if (isExpired)
                    {
                        SafepayDbHelper.UpdateAttemptStatus(tracker, "Expired");
                        TempData["CheckoutError"] =
                            "Payment session expired. Your cart is preserved — please try again.";
                    }
                    else
                    {
                        TempData["CheckoutError"] =
                            "Payment was not completed. Your cart is preserved — please try again or choose another payment method.";
                    }

                    return RedirectToAction("Checkout", "Cart");
                }
            }
        }


        // ─── 3. PaymentCancelled (GET) ────────────────────────────────────────

        /// <summary>
        /// Safepay redirects here when the user cancels or abandons the checkout.
        ///
        /// Route: GET /Safepay/PaymentCancelled?paymentId=X&amp;tracker=Y
        ///
        /// Cart is NEVER cleared on cancellation.
        /// </summary>
        [SessionAuthFilter]
        [HttpGet]
        public ActionResult PaymentCancelled(int paymentId, string tracker)
        {
            int userId = SessionHelper.GetUserId();

            // Keep only the first real tracker token (see NormalizeTracker).
            tracker = NormalizeTracker(tracker);

            using (var db = new Demo_DevDBEntities())
            {
                var payment = db.Payments
                                .Include(p => p.Booking)
                                .FirstOrDefault(p => p.PaymentId == paymentId);

                if (payment != null
                    && payment.Booking != null
                    && payment.Booking.UserId == userId
                    && !string.IsNullOrWhiteSpace(tracker))
                {
                    var attempt = SafepayDbHelper.GetAttemptByToken(tracker);
                    if (attempt != null && attempt.PaymentId == paymentId)
                    {
                        SafepayDbHelper.UpdateAttemptStatus(tracker, "Cancelled");
                    }
                }
            }

            // Cart is NEVER cleared
            TempData["CheckoutError"] =
                "Payment was cancelled. Your cart is preserved — you can try again.";
            return RedirectToAction("Checkout", "Cart");
        }


        // ─── 4. Webhook (POST) ────────────────────────────────────────────────

        /// <summary>
        /// Public endpoint called by Safepay servers to push asynchronous payment events.
        /// No session authentication — Safepay calls this server-to-server.
        ///
        /// Route: POST /Safepay/Webhook
        ///
        /// Security:
        ///   - Raw body is read before MVC model-binding
        ///   - HMAC-SHA512 signature verified against X-SFPY-SIGNATURE header
        ///   - Idempotent: returns 200 immediately if tracker already Paid
        ///   - Never downgrades a Completed payment
        ///   - payment.failed event does NOT mark local payment Failed (non-terminal attempt)
        /// </summary>
        [HttpPost]
        public ActionResult Webhook()
        {
            // ── 1. Read raw body BEFORE MVC model-binding touches it ────────────
            Request.InputStream.Seek(0, SeekOrigin.Begin);
            string rawJsonBody;
            using (var reader = new StreamReader(
                       Request.InputStream,
                       Encoding.UTF8,
                       detectEncodingFromByteOrderMarks: false,
                       bufferSize: 8192,
                       leaveOpen: true))
            {
                rawJsonBody = reader.ReadToEnd();
            }

            // ── 2. Read signature header ─────────────────────────────────────────
            string signature = Request.Headers["X-SFPY-SIGNATURE"];

            // ── 3. Verify HMAC-SHA512 signature ──────────────────────────────────
            if (!SafepayService.VerifyWebhookSignature(rawJsonBody, signature))
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { error = "invalid signature" });
            }

            // ── 4. Parse JSON body ───────────────────────────────────────────────
            JObject body;
            try
            {
                body = JObject.Parse(rawJsonBody);
            }
            catch (JsonException)
            {
                // Return 200 so Safepay does not retry a malformed payload indefinitely
                return Json(new { received = true, warning = "unparseable body" });
            }

            // ── 5. Extract event type, tracker token and state ───────────────────
            string eventType    = body["type"]?.ToString();
            string trackerToken = body["data"]?["tracker"]?.ToString();
            string state        = body["data"]?["state"]?.ToString();

            long   webhookAmount   = 0;
            string webhookCurrency = body["data"]?["currency"]?.ToString();
            var    amtToken        = body["data"]?["amount"];
            if (amtToken != null && amtToken.Type != JTokenType.Null)
                long.TryParse(amtToken.ToString(), out webhookAmount);

            if (string.IsNullOrWhiteSpace(trackerToken))
            {
                return Json(new { received = true });
            }

            // ── 6. Idempotency guard ─────────────────────────────────────────────
            if (SafepayDbHelper.IsTokenAlreadyPaid(trackerToken))
                return Json(new { received = true });

            // ── 7. Process by event type and state ───────────────────────────────

            // payment.succeeded + TRACKER_ENDED → confirm order
            if (string.Equals(eventType, "payment.succeeded", StringComparison.OrdinalIgnoreCase)
                && string.Equals(state, StateEnded, StringComparison.OrdinalIgnoreCase))
            {
                var attempt = SafepayDbHelper.GetAttemptByToken(trackerToken);
                if (attempt != null)
                {
                    long expectedPaisas = (long)Math.Round(attempt.Amount * 100m, MidpointRounding.AwayFromZero);

                    bool amountOk   = webhookAmount == 0 || webhookAmount == expectedPaisas;
                    bool currencyOk = string.IsNullOrWhiteSpace(webhookCurrency)
                                   || string.Equals(webhookCurrency, "PKR", StringComparison.OrdinalIgnoreCase);

                    if (amountOk && currencyOk)
                    {
                        using (var db = new Demo_DevDBEntities())
                        {
                            var payment = db.Payments
                                            .Include(p => p.Booking)
                                            .FirstOrDefault(p => p.PaymentId == attempt.PaymentId);

                            if (payment != null && payment.Booking != null)
                            {
                                if (!string.Equals(payment.Status, "Completed",
                                                   StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var tx = db.Database.BeginTransaction())
                                    {
                                        try
                                        {
                                            payment.Status         = "Completed";
                                            payment.Booking.Status = "Confirmed";
                                            db.SaveChanges();

                                            int userId = payment.Booking.UserId;
                                            ClearOrderedCartItems(db, attempt.BookingId, userId);

                                            tx.Commit();
                                        }
                                        catch
                                        {
                                            tx.Rollback();
                                            throw;
                                        }
                                    }
                                }
                            }
                        }

                        SafepayDbHelper.UpdateAttemptStatus(trackerToken, "Paid", rawJsonBody);
                    }
                    // If amount/currency mismatch: log but return 200 (do not confirm payment)
                }
            }
            else if (string.Equals(eventType, "payment.failed", StringComparison.OrdinalIgnoreCase))
            {
                bool isTrackerCancelled = string.Equals(state, StateCancelled,
                                                        StringComparison.OrdinalIgnoreCase);
                bool isTrackerExpired   = string.Equals(state, StateExpired,
                                                        StringComparison.OrdinalIgnoreCase);

                if (isTrackerCancelled)
                    SafepayDbHelper.UpdateAttemptStatus(trackerToken, "Cancelled", rawJsonBody);
                else if (isTrackerExpired)
                    SafepayDbHelper.UpdateAttemptStatus(trackerToken, "Expired", rawJsonBody);
                // Otherwise: individual attempt failed but tracker may still succeed.
            }
            else if (string.Equals(state, StateCancelled, StringComparison.OrdinalIgnoreCase))
            {
                SafepayDbHelper.UpdateAttemptStatus(trackerToken, "Cancelled", rawJsonBody);
            }
            else if (string.Equals(state, StateExpired, StringComparison.OrdinalIgnoreCase))
            {
                SafepayDbHelper.UpdateAttemptStatus(trackerToken, "Expired", rawJsonBody);
            }

            // Always return HTTP 200 — Safepay retries on non-200 responses
            return Json(new { received = true });
        }


        // ─── Helper: ThankYou TempData ────────────────────────────────────────

        /// <summary>
        /// Populates the TempData keys expected by Cart/ThankYou.cshtml.
        /// Mirrors exactly the keys set by CartController.PlaceOrder for consistency.
        /// </summary>
        private void SetThankYouTempData(Payment payment, int bookingId, Demo_DevDBEntities db)
        {
            var booking = payment.Booking
                       ?? db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);

            bool hasPackages = db.BookingDetails
                                 .Any(d => d.BookingId == bookingId && d.PackageId.HasValue);
            bool hasProducts = db.BookingDetails
                                 .Any(d => d.BookingId == bookingId && d.ProductId.HasValue);

            string customerName = "Valued Customer";
            if (booking?.Email != null)
                customerName = booking.Email;

            TempData["BookingId"]     = bookingId;
            TempData["TotalAmount"]   = payment.Amount;
            TempData["CustomerName"]  = customerName;
            TempData["PaymentMethod"] = payment.PaymentMethod ?? "Credit/Debit Card";

            if (hasPackages && hasProducts)
                TempData["OrderType"] = "mixed";
            else if (hasPackages)
                TempData["OrderType"] = "packages";
            else
                TempData["OrderType"] = "products";

            if (hasPackages)
            {
                // TravelDate is stored in BookingDate; Travelers was submitted via form.
                TempData["TravelDate"] = booking != null && booking.BookingDate.HasValue
                    ? booking.BookingDate.Value.ToString("yyyy-MM-dd")
                    : string.Empty;
                TempData["Travelers"]  = 1; // actual value was in the submitted form
            }
        }
    }
}