using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TourwebsiteFYP.Services
{
    // ─── Result DTOs ──────────────────────────────────────────────────────────

    public class SafepayTrackerResult
    {
        public string TrackerToken { get; set; }
        public bool   Success      { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SafepayTbtResult
    {
        public string TbtToken     { get; set; }
        public bool   Success      { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SafepayTrackerStateResult
    {
        /// <summary>
        /// Tracker token from data.tracker.token in the reporter API response.
        /// Ref: GET /reporter/api/v1/payments/{tracker}
        ///   Response path: data.tracker.token
        /// </summary>
        public string TrackerToken { get; set; }

        /// <summary>
        /// Tracker state from data.tracker.state.
        /// "TRACKER_ENDED" = successful payment.
        /// </summary>
        public string State        { get; set; }

        /// <summary>
        /// Verified payment amount in smallest currency unit (paisas for PKR).
        /// From data.tracker.purchase_totals.quote_amount.amount
        /// </summary>
        public long   Amount       { get; set; }

        /// <summary>
        /// Verified payment currency (e.g. "PKR").
        /// From data.tracker.purchase_totals.quote_amount.currency
        /// </summary>
        public string Currency     { get; set; }

        public bool   Success      { get; set; }
        public string ErrorMessage { get; set; }
    }

    // ─── Service ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Static service for interacting with the Safepay payment gateway.
    ///
    /// API references:
    ///   Tracker creation : POST /order/payments/v3/
    ///     Ref: https://safepay-docs.netlify.app/build-your-integration/express-checkout/
    ///   Passport token   : POST /client/passport/v1/token
    ///   Tracker verify   : GET  /reporter/api/v1/payments/{tracker}
    ///     Response path  : data.tracker.token / .state / .purchase_totals.quote_amount
    ///   Checkout URL     : {CheckoutBase}/embedded/?environment=&tracker=&tbt=&source=hosted
    ///                                              &redirect_url=&cancel_url=
    ///     This is the route used by the current official Safepay SDK for trackers
    ///     created with the v3 order API. The older /components?beacon= route belongs
    ///     to the legacy v1 flow and sends v3 trackers to the Safepay marketing page.
    ///   Webhook HMAC     : HMAC-SHA512(rawBody), key = UTF8(webhookSecret)
    ///                      Header X-SFPY-SIGNATURE = lowercase hex digest (no prefix)
    ///     NOTE: not yet confirmed with a real sandbox webhook. Test before go live.
    ///
    /// Credentials (all stored in Web.secrets.config, never in Web.config):
    ///   Safepay:SecretKey     — private merchant secret (X-SFPY-MERCHANT-SECRET header)
    ///   Safepay:PublicKey     — public merchant API key (request body: merchant_api_key)
    ///   Safepay:WebhookSecret — webhook signing secret (UTF-8 string, NOT Base64)
    ///   Safepay:Environment   — "sandbox" or "production"
    /// </summary>
    public static class SafepayService
    {
        // ── Config keys ───────────────────────────────────────────────────────
        private const string CfgSecretKey     = "Safepay:SecretKey";
        private const string CfgPublicKey     = "Safepay:PublicKey";
        private const string CfgWebhookSecret = "Safepay:WebhookSecret";
        private const string CfgEnvironment   = "Safepay:Environment";

        // ── API base URLs ─────────────────────────────────────────────────────
        // Source: official Safepay developer documentation
        private const string SandboxApiBase    = "https://sandbox.api.getsafepay.com";
        private const string ProductionApiBase = "https://api.getsafepay.com";

        // ── Hosted checkout base URLs ─────────────────────────────────────────
        // Sandbox: same host as the API (confirmed from the current SDK behaviour).
        // Production: NOT verified against a live account. Confirm before go live.
        private const string SandboxCheckoutBase    = "https://sandbox.api.getsafepay.com";
        private const string ProductionCheckoutBase = "https://getsafepay.com";

        // ── Merchant auth header ──────────────────────────────────────────────
        // Source: Safepay docs — server-to-server auth uses X-SFPY-MERCHANT-SECRET
        private const string MerchantSecretHeader = "X-SFPY-MERCHANT-SECRET";

        // ── Credential accessors ──────────────────────────────────────────────
        /// <summary>Private merchant secret — used in X-SFPY-MERCHANT-SECRET header only.</summary>
        private static string SecretKey
            => ConfigurationManager.AppSettings[CfgSecretKey] ?? string.Empty;

        /// <summary>Public merchant API key — goes inside request body as merchant_api_key.</summary>
        private static string PublicKey
            => ConfigurationManager.AppSettings[CfgPublicKey] ?? string.Empty;

        /// <summary>
        /// Webhook signing secret — raw UTF-8 string as copied from Safepay dashboard.
        /// Do NOT Base64-decode it; the secret is used directly as the HMAC key bytes.
        /// </summary>
        private static string WebhookSecret
            => ConfigurationManager.AppSettings[CfgWebhookSecret] ?? string.Empty;

        private static string Environment
            => ConfigurationManager.AppSettings[CfgEnvironment] ?? "sandbox";

        private static bool IsProduction
            => string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase);

        // ── Derived base URLs ─────────────────────────────────────────────────
        private static string ApiBase
            => IsProduction ? ProductionApiBase : SandboxApiBase;

        private static string CheckoutBase
            => IsProduction ? ProductionCheckoutBase : SandboxCheckoutBase;

        // ── HTTP client factory ───────────────────────────────────────────────
        /// <summary>
        /// Creates an HttpClient with the merchant-secret header and 30s timeout.
        /// NOTE: Per-call creation is intentional for .NET 4.7.2 (no IHttpClientFactory).
        /// </summary>
        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add(MerchantSecretHeader, SecretKey);
            return client;
        }

        // ── 1. CreateTracker ──────────────────────────────────────────────────

        /// <summary>
        /// Creates a Safepay payment tracker.
        ///
        /// Ref: POST /order/payments/v3/
        ///   Header : X-SFPY-MERCHANT-SECRET  (private secret key)
        ///   Body   : {
        ///               "merchant_api_key": "&lt;public key&gt;",
        ///               "intent":           "CYBERSOURCE",
        ///               "mode":             "payment",
        ///               "currency":         "PKR",
        ///               "amount":           &lt;paisas&gt;
        ///            }
        ///   Returns: { "data": { "tracker": { "token": "track_...", "state": "TRACKER_STARTED" } } }
        ///
        /// Amount is in smallest currency unit: 1 PKR = 100 paisas.
        /// </summary>
        public static SafepayTrackerResult CreateTracker(decimal amountInPkr, string orderId)
        {
            try
            {
                long amountInPaisas = (long)Math.Round(amountInPkr * 100m, MidpointRounding.AwayFromZero);

                var payload = new
                {
                    merchant_api_key = PublicKey,
                    intent           = "CYBERSOURCE",
                    mode             = "payment",
                    currency         = "PKR",
                    amount           = amountInPaisas
                };

                string json = JsonConvert.SerializeObject(payload);
                Debug.WriteLine("[SafepayService] CreateTracker → POST " + ApiBase + "/order/payments/v3/");

                using (var client = CreateHttpClient())
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = client.PostAsync(ApiBase + "/order/payments/v3/", content).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine("[SafepayService] CreateTracker ← HTTP " + (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Log sanitized error body for diagnostics (never logs credentials)
                        string sanitized = responseBody.Length > 300
                            ? responseBody.Substring(0, 300) + "…"
                            : responseBody;
                        Debug.WriteLine("[SafepayService] CreateTracker ERROR body: " + sanitized);

                        return new SafepayTrackerResult
                        {
                            Success      = false,
                            ErrorMessage = "HTTP " + (int)response.StatusCode
                                         + " — " + sanitized
                        };
                    }

                    // Response: { "data": { "tracker": { "token": "track_...", "state": "TRACKER_STARTED" } } }
                    var    jObj = JObject.Parse(responseBody);
                    string tok  = jObj["data"]?["tracker"]?["token"]?.ToString();

                    if (string.IsNullOrEmpty(tok))
                    {
                        return new SafepayTrackerResult
                        {
                            Success      = false,
                            ErrorMessage = "Tracker token missing from Safepay response (expected data.tracker.token)."
                        };
                    }

                    return new SafepayTrackerResult { TrackerToken = tok, Success = true };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SafepayService] CreateTracker EXCEPTION: " + ex.GetType().Name);
                return new SafepayTrackerResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── 2. GetPassportToken ───────────────────────────────────────────────

        /// <summary>
        /// Obtains a short-lived client passport token (TBT) used in the checkout URL.
        ///
        /// Ref: POST /client/passport/v1/token
        ///   Header : X-SFPY-MERCHANT-SECRET
        ///   Body   : {} (empty JSON object)
        ///   Returns: { "data": "&lt;tbt-string&gt;" }
        ///
        /// Token is the string value of "data" directly — NOT data.token.
        /// The token lasts one hour, and the hosted page expires with it.
        /// </summary>
        public static SafepayTbtResult GetPassportToken()
        {
            try
            {
                string json = "{}";
                Debug.WriteLine("[SafepayService] GetPassportToken → POST " + ApiBase + "/client/passport/v1/token");

                using (var client = CreateHttpClient())
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = client.PostAsync(ApiBase + "/client/passport/v1/token", content).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine("[SafepayService] GetPassportToken ← HTTP " + (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new SafepayTbtResult
                        {
                            Success      = false,
                            ErrorMessage = "HTTP " + (int)response.StatusCode + " from Safepay passport token."
                        };
                    }

                    var    jObj = JObject.Parse(responseBody);
                    string tbt  = jObj["data"]?.ToString();

                    if (string.IsNullOrEmpty(tbt))
                    {
                        return new SafepayTbtResult
                        {
                            Success      = false,
                            ErrorMessage = "Passport token missing from Safepay response."
                        };
                    }

                    return new SafepayTbtResult { TbtToken = tbt, Success = true };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SafepayService] GetPassportToken EXCEPTION: " + ex.GetType().Name);
                return new SafepayTbtResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── 3. BuildCheckoutUrl ───────────────────────────────────────────────

        /// <summary>
        /// Builds the Safepay hosted checkout URL to redirect the customer to.
        ///
        ///   Base URL : {CheckoutBase}/embedded/
        ///   Parameters (same order as the official SDK):
        ///     environment  — "sandbox" or "production"
        ///     tracker      — tracker token from CreateTracker
        ///     tbt          — passport token from GetPassportToken
        ///     source       — "hosted"
        ///     redirect_url — absolute URL Safepay sends the customer to after payment
        ///     cancel_url   — absolute URL Safepay sends the customer to on cancel
        ///
        /// No "beacon", "order_id" or "webhooks" parameters. Those belong to the
        /// legacy /components route, which does not accept v3 trackers.
        /// </summary>
        public static string BuildCheckoutUrl(
            string trackerToken,
            string tbtToken,
            string redirectUrl,
            string cancelUrl)
        {
            var sb = new StringBuilder(CheckoutBase);
            sb.Append("/embedded/?environment=");
            sb.Append(HttpUtility.UrlEncode(IsProduction ? "production" : "sandbox"));
            sb.Append("&tracker=");
            sb.Append(HttpUtility.UrlEncode(trackerToken));
            sb.Append("&tbt=");
            sb.Append(HttpUtility.UrlEncode(tbtToken));
            sb.Append("&source=hosted");
            sb.Append("&redirect_url=");
            sb.Append(HttpUtility.UrlEncode(redirectUrl));
            sb.Append("&cancel_url=");
            sb.Append(HttpUtility.UrlEncode(cancelUrl));

            // Only host and path are logged. Tokens and query values are never logged.
            Debug.WriteLine("[SafepayService] BuildCheckoutUrl → " + CheckoutBase + "/embedded/ (query not logged)");
            return sb.ToString();
        }

        // ── 4. VerifyTracker ──────────────────────────────────────────────────

        /// <summary>
        /// Fetches the current state of a tracker from Safepay's reporter API.
        ///
        /// Ref: GET /reporter/api/v1/payments/{tracker}
        ///   Header : X-SFPY-MERCHANT-SECRET
        ///   Response structure (from the Express Checkout guide):
        ///   {
        ///     "data": {
        ///       "tracker": {
        ///         "token": "track_...",
        ///         "state": "TRACKER_ENDED",
        ///         "purchase_totals": {
        ///           "quote_amount": { "currency": "PKR", "amount": 290000 }
        ///         }
        ///       }
        ///     }
        ///   }
        ///
        /// Amount is in paisas (smallest unit): 290000 = PKR 2,900.00
        /// </summary>
        public static SafepayTrackerStateResult VerifyTracker(string trackerToken)
        {
            try
            {
                string url = ApiBase + "/reporter/api/v1/payments/" + HttpUtility.UrlEncode(trackerToken);
                Debug.WriteLine("[SafepayService] VerifyTracker → GET /reporter/api/v1/payments/{tracker}");

                using (var client = CreateHttpClient())
                using (var response = client.GetAsync(url).Result)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine("[SafepayService] VerifyTracker ← HTTP " + (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new SafepayTrackerStateResult
                        {
                            Success      = false,
                            ErrorMessage = "HTTP " + (int)response.StatusCode + " from Safepay reporter: "
                                         + (responseBody != null && responseBody.Length > 200
                                                ? responseBody.Substring(0, 200)
                                                : responseBody)
                        };
                    }

                    var jObj = JObject.Parse(responseBody);
                    var data = jObj["data"];

                    // Documented shape is data.tracker. Also accept a flat data object
                    // that holds the state directly, since the shape was not confirmed live.
                    var tracker = data?["tracker"] ?? (data?["state"] != null ? data : null);

                    if (tracker == null)
                    {
                        // Only key names are reported, never values.
                        string keys = data is JObject dobj
                            ? string.Join(",", dobj.Properties().Select(p => p.Name))
                            : "none";

                        return new SafepayTrackerStateResult
                        {
                            Success      = false,
                            ErrorMessage = "Reporter API returned no tracker data. Keys under data: " + keys
                        };
                    }

                    string token = tracker["token"]?.ToString();
                    string state = tracker["state"]?.ToString();

                    var    quoteAmount = tracker["purchase_totals"]?["quote_amount"];
                    string currency    = quoteAmount?["currency"]?.ToString();
                    long   amount      = 0;

                    var amtToken = quoteAmount?["amount"];
                    if (amtToken != null && amtToken.Type != JTokenType.Null)
                        long.TryParse(amtToken.ToString(), out amount);

                    return new SafepayTrackerStateResult
                    {
                        TrackerToken = token,
                        State        = state,
                        Amount       = amount,
                        Currency     = currency,
                        Success      = true
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SafepayService] VerifyTracker EXCEPTION: " + ex.GetType().Name);
                return new SafepayTrackerStateResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── 5. VerifyWebhookSignature ──────────────────────────────────────────

        /// <summary>
        /// Verifies the HMAC-SHA512 signature of an incoming Safepay webhook.
        ///
        ///   Algorithm   : HMAC-SHA512
        ///   Key         : UTF-8 bytes of the webhook secret (raw string — NOT Base64-decoded)
        ///   Message     : raw request body (JSON string as-is — no timestamp prepended)
        ///   Header sent : X-SFPY-SIGNATURE = lowercase hex digest
        ///   Comparison  : constant-time byte comparison
        ///
        /// NOTE: this scheme has not been confirmed with a real sandbox webhook.
        /// Send a test event from the Safepay dashboard and check it passes.
        /// Never log the computed signature or the webhook secret.
        /// </summary>
        public static bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            try
            {
                if (string.IsNullOrEmpty(rawBody) || string.IsNullOrEmpty(signatureHeader))
                {
                    Debug.WriteLine("[SafepayService] VerifyWebhookSignature: missing parameters.");
                    return false;
                }

                string secret = WebhookSecret;
                if (string.IsNullOrEmpty(secret))
                {
                    Debug.WriteLine("[SafepayService] VerifyWebhookSignature: WebhookSecret not configured.");
                    return false;
                }

                byte[] keyBytes     = Encoding.UTF8.GetBytes(secret);
                byte[] messageBytes = Encoding.UTF8.GetBytes(rawBody);

                string computedHex;
                using (var hmac = new HMACSHA512(keyBytes))
                {
                    byte[] hash = hmac.ComputeHash(messageBytes);
                    computedHex = BitConverter.ToString(hash)
                                              .Replace("-", "")
                                              .ToLowerInvariant();
                }

                return ConstantTimeEquals(computedHex, signatureHeader.Trim());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SafepayService] VerifyWebhookSignature EXCEPTION: " + ex.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Compares two strings in constant time to prevent timing-based attacks.
        /// Returns true only when both strings are identical in length and content.
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;

            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);

            int result = aBytes.Length ^ bBytes.Length;
            int len    = Math.Min(aBytes.Length, bBytes.Length);
            for (int i = 0; i < len; i++)
                result |= aBytes[i] ^ bBytes[i];

            return result == 0;
        }
    }
}