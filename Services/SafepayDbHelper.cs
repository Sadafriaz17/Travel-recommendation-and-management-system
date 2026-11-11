using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity.Core.EntityClient;
using TourwebsiteFYP.Models;

namespace TourwebsiteFYP.Services
{
    /// <summary>
    /// Raw ADO.NET helper for Safepay-related database operations.
    ///
    /// All methods use the SQL connection string extracted from the EF entity
    /// connection string — EDMX auto-generated files are never modified.
    ///
    /// Status values: Initiated → Paid | Failed | Cancelled | Expired
    /// Terminal states: Paid, Failed, Cancelled, Expired
    /// Guard: once a row is Paid it can never transition to any other status.
    /// </summary>
    public static class SafepayDbHelper
    {
        // ── Connection string ─────────────────────────────────────────────────

        private static string GetSqlConnectionString()
        {
            var efConnStr = ConfigurationManager
                                .ConnectionStrings["Demo_DevDBEntities"]
                                .ConnectionString;
            var ecb = new EntityConnectionStringBuilder(efConnStr);
            return ecb.ProviderConnectionString;
        }

        // ── Terminal-state guard ──────────────────────────────────────────────

        // Paid is the only state from which NO transition is allowed.
        // Failed, Cancelled and Expired are terminal but not upgrade-protected
        // from each other (a webhook may arrive after a cancellation).
        private static readonly string[] NoDowngradeFrom = { "Paid" };

        // ── 1. InsertAttempt ──────────────────────────────────────────────────

        /// <summary>
        /// Inserts a new SafepayPaymentAttempt row and returns the generated
        /// SafepayAttemptId (SCOPE_IDENTITY).
        /// </summary>
        public static int InsertAttempt(
            int     paymentId,
            int     bookingId,
            string  trackerToken,
            decimal amount,
            string  currency = "PKR")
        {
            if (string.IsNullOrWhiteSpace(trackerToken))
                throw new ArgumentNullException("trackerToken");

            const string sql = @"
INSERT INTO dbo.SafepayPaymentAttempt
    (PaymentId, BookingId, TrackerToken, Amount, Currency, Status, CreatedAt)
VALUES
    (@PaymentId, @BookingId, @TrackerToken, @Amount, @Currency, 'Initiated', GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (var conn = new SqlConnection(GetSqlConnectionString()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@PaymentId",    SqlDbType.Int).Value           = paymentId;
                cmd.Parameters.Add("@BookingId",    SqlDbType.Int).Value           = bookingId;
                cmd.Parameters.Add("@TrackerToken", SqlDbType.NVarChar, 255).Value = trackerToken;
                var amtParam = cmd.Parameters.Add("@Amount", SqlDbType.Decimal);
                amtParam.Value     = amount;
                amtParam.Precision = 18;
                amtParam.Scale     = 2;
                cmd.Parameters.Add("@Currency", SqlDbType.NVarChar, 10).Value =
                    string.IsNullOrWhiteSpace(currency) ? "PKR" : currency;

                conn.Open();
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // ── 2. UpdateAttemptStatus ────────────────────────────────────────────

        /// <summary>
        /// Updates the Status of a SafepayPaymentAttempt row identified by trackerToken.
        ///
        /// Guard (atomic): uses a single UPDATE with a WHERE clause that rejects
        /// the write if the current status is already 'Paid'. This prevents the
        /// race condition where a concurrent request could read "not Paid", then
        /// both write conflicting statuses.
        ///
        /// Timestamp rules:
        ///   - CompletedAt    is set when transitioning TO Paid, Failed, Cancelled or Expired.
        ///   - WebhookReceivedAt is set only when rawWebhookPayload is provided (i.e. from webhook).
        ///   - Neither field is ever reset once written.
        /// </summary>
        public static void UpdateAttemptStatus(
            string trackerToken,
            string status,
            string rawWebhookPayload = null)
        {
            if (string.IsNullOrWhiteSpace(trackerToken))
                throw new ArgumentNullException("trackerToken");
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentNullException("status");

            bool isTerminal  = IsTerminalStatus(status);
            bool hasWebhook  = rawWebhookPayload != null;

            // Single atomic UPDATE:
            //   WHERE Status <> 'Paid'  — prevents any downgrade from Paid
            //   CompletedAt: set only on first terminal transition (CASE preserves existing)
            //   WebhookReceivedAt: set only when webhook payload is supplied
            const string sql = @"
UPDATE dbo.SafepayPaymentAttempt
SET
    Status            = @Status,
    CompletedAt       = CASE
                            WHEN @IsTerminal = 1 AND CompletedAt IS NULL
                            THEN GETDATE()
                            ELSE CompletedAt
                        END,
    WebhookReceivedAt = CASE
                            WHEN @HasWebhook = 1 AND WebhookReceivedAt IS NULL
                            THEN GETDATE()
                            ELSE WebhookReceivedAt
                        END,
    RawWebhookPayload = CASE
                            WHEN @HasWebhook = 1
                            THEN @RawWebhookPayload
                            ELSE RawWebhookPayload
                        END
WHERE  TrackerToken = @TrackerToken
  AND  Status      <> 'Paid';";

            using (var conn = new SqlConnection(GetSqlConnectionString()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Status",            SqlDbType.NVarChar, 50).Value  = status;
                cmd.Parameters.Add("@TrackerToken",      SqlDbType.NVarChar, 255).Value = trackerToken;
                cmd.Parameters.Add("@IsTerminal",        SqlDbType.Bit).Value           = isTerminal ? 1 : 0;
                cmd.Parameters.Add("@HasWebhook",        SqlDbType.Bit).Value           = hasWebhook ? 1 : 0;
                cmd.Parameters.Add("@RawWebhookPayload", SqlDbType.NVarChar, -1).Value  =
                    hasWebhook ? (object)rawWebhookPayload : DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
                // rowsAffected == 0 is benign: it means Status was already 'Paid', which is correct.
            }
        }

        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, "Paid",       StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Failed",     StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Cancelled",  StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Expired",    StringComparison.OrdinalIgnoreCase);
        }

        // ── 3. GetAttemptByToken ──────────────────────────────────────────────

        /// <summary>
        /// Returns the SafepayAttemptDto for the given tracker token, or null if not found.
        /// </summary>
        public static SafepayAttemptDto GetAttemptByToken(string trackerToken)
        {
            if (string.IsNullOrWhiteSpace(trackerToken))
                throw new ArgumentNullException("trackerToken");

            const string sql = @"
SELECT TOP 1
    SafepayAttemptId, PaymentId, BookingId, TrackerToken,
    Amount, Currency, Status, CreatedAt, CompletedAt,
    WebhookReceivedAt, RawWebhookPayload
FROM dbo.SafepayPaymentAttempt
WHERE TrackerToken = @TrackerToken;";

            using (var conn = new SqlConnection(GetSqlConnectionString()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@TrackerToken", SqlDbType.NVarChar, 255).Value = trackerToken;

                conn.Open();
                using (var r = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!r.Read()) return null;

                    return new SafepayAttemptDto
                    {
                        SafepayAttemptId  = r.GetInt32(r.GetOrdinal("SafepayAttemptId")),
                        PaymentId         = r.GetInt32(r.GetOrdinal("PaymentId")),
                        BookingId         = r.GetInt32(r.GetOrdinal("BookingId")),
                        TrackerToken      = r.GetString(r.GetOrdinal("TrackerToken")),
                        Amount            = r.GetDecimal(r.GetOrdinal("Amount")),
                        Currency          = r.GetString(r.GetOrdinal("Currency")),
                        Status            = r.GetString(r.GetOrdinal("Status")),
                        CreatedAt         = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                        CompletedAt       = r.IsDBNull(r.GetOrdinal("CompletedAt"))
                                                ? (DateTime?)null
                                                : r.GetDateTime(r.GetOrdinal("CompletedAt")),
                        WebhookReceivedAt = r.IsDBNull(r.GetOrdinal("WebhookReceivedAt"))
                                                ? (DateTime?)null
                                                : r.GetDateTime(r.GetOrdinal("WebhookReceivedAt")),
                        RawWebhookPayload = r.IsDBNull(r.GetOrdinal("RawWebhookPayload"))
                                                ? null
                                                : r.GetString(r.GetOrdinal("RawWebhookPayload"))
                    };
                }
            }
        }

        // ── 4. IsTokenAlreadyPaid ─────────────────────────────────────────────

        /// <summary>
        /// Returns true if the attempt identified by trackerToken has Status = 'Paid'.
        /// Used for idempotency checks in both PaymentSuccess and Webhook handlers.
        /// </summary>
        public static bool IsTokenAlreadyPaid(string trackerToken)
        {
            if (string.IsNullOrWhiteSpace(trackerToken))
                throw new ArgumentNullException("trackerToken");

            const string sql = @"
SELECT TOP 1 1
FROM dbo.SafepayPaymentAttempt
WHERE TrackerToken = @TrackerToken
  AND Status       = 'Paid';";

            using (var conn = new SqlConnection(GetSqlConnectionString()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@TrackerToken", SqlDbType.NVarChar, 255).Value = trackerToken;

                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        // ── 5. UpdatePaymentSafepayTracker ────────────────────────────────────

        /// <summary>
        /// Stamps the SafepayTracker column on the Payment row identified by paymentId.
        /// Uses a raw SQL UPDATE so the EDMX does not need to be regenerated just for
        /// this new column.
        /// </summary>
        public static void UpdatePaymentSafepayTracker(int paymentId, string trackerToken)
        {
            if (string.IsNullOrWhiteSpace(trackerToken))
                throw new ArgumentNullException("trackerToken");

            const string sql = @"
UPDATE dbo.Payment
SET    SafepayTracker = @tracker
WHERE  PaymentId      = @id;";

            using (var conn = new SqlConnection(GetSqlConnectionString()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@tracker", SqlDbType.NVarChar, 255).Value = trackerToken;
                cmd.Parameters.Add("@id",      SqlDbType.Int).Value           = paymentId;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
