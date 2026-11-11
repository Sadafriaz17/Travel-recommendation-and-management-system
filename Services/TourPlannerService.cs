
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TourwebsiteFYP.Services
{
    /*
    =========================================================
    LEGACY TOUR PLANNER SERVICE
    =========================================================

    Existing daily itinerary generation service.

    This service is separate from:
    1. NexVoy destination recommendation API.
    2. New NexVoy Packing Assistant API.

    Existing API request/response contract is preserved.
    =========================================================
    */

    // =========================================================
    // REQUEST DTO
    // =========================================================

    public class TourPlannerRequest
    {
        [JsonProperty("fromCity")]
        public string FromCity { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("endDate")]
        public string EndDate { get; set; }

        [JsonProperty("budgetMin")]
        public decimal BudgetMin { get; set; }

        [JsonProperty("budgetMax")]
        public decimal BudgetMax { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("travelers")]
        public int Travelers { get; set; }

        [JsonProperty("interests")]
        public List<string> Interests { get; set; }

        [JsonProperty("travelStyle")]
        public string TravelStyle { get; set; }

        [JsonProperty("healthNotes")]
        public string HealthNotes { get; set; }
    }

    // =========================================================
    // RESPONSE DTOs
    // =========================================================

    public class TourPlannerResponse
    {
        [JsonProperty("totalBudget")]
        public BudgetSummary TotalBudget { get; set; }

        [JsonProperty("healthNotes")]
        public string HealthNotes { get; set; }

        [JsonProperty("days")]
        public List<TourDay> Days { get; set; }
    }

    public class BudgetSummary
    {
        [JsonProperty("estimated")]
        public decimal Estimated { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("breakdown")]
        public BudgetBreakdown Breakdown { get; set; }
    }

    public class BudgetBreakdown
    {
        [JsonProperty("accommodation")]
        public decimal Accommodation { get; set; }

        [JsonProperty("food")]
        public decimal Food { get; set; }

        [JsonProperty("transport")]
        public decimal Transport { get; set; }

        [JsonProperty("activities")]
        public decimal Activities { get; set; }

        [JsonProperty("misc")]
        public decimal Misc { get; set; }
    }

    public class TourDay
    {
        [JsonProperty("dayNumber")]
        public int DayNumber { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("slots")]
        public List<TourSlot> Slots { get; set; }
    }

    public class TourSlot
    {
        [JsonProperty("period")]
        public string Period { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("place")]
        public string Place { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("estimatedCost")]
        public decimal EstimatedCost { get; set; }
    }

    // =========================================================
    // SERVICE RESULT
    // =========================================================

    public class TourPlannerServiceResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public TourPlannerResponse Data { get; set; }
    }

    // =========================================================
    // TOUR PLANNER SERVICE
    // =========================================================

    public class TourPlannerService
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12;

            return new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        // =====================================================
        // GENERATE ITINERARY
        // =====================================================

        public async Task<TourPlannerServiceResult> GenerateAsync(
            TourPlannerRequest req)
        {
            if (!AiSettings.IsTourReady)
            {
                return Fail(
                    "AI is not connected yet. Please check the Tour Planner API configuration."
                );
            }

            if (req == null)
            {
                return Fail("Invalid Tour Planner request.");
            }

            // Sanitize free-text fields.
            req.FromCity = Sanitise(req.FromCity, 200);
            req.Destination = Sanitise(req.Destination, 200);
            req.HealthNotes = Sanitise(req.HealthNotes, 500);
            req.Currency = Sanitise(req.Currency, 5);

            if (string.IsNullOrWhiteSpace(req.Destination))
            {
                return Fail("Please enter a destination.");
            }

            if (string.IsNullOrWhiteSpace(req.FromCity))
            {
                return Fail("Please enter your departure city.");
            }

            if (req.Travelers < 1 || req.Travelers > 20)
            {
                return Fail("Number of travelers must be between 1 and 20.");
            }

            if (req.BudgetMin < 0 || req.BudgetMax < 0)
            {
                return Fail("Budget cannot be negative.");
            }

            if (req.BudgetMax > 0 && req.BudgetMin > req.BudgetMax)
            {
                return Fail("Maximum budget must be greater than minimum budget.");
            }

            if (req.Interests == null)
            {
                req.Interests = new List<string>();
            }

            // Validate trip dates.
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParse(req.StartDate, out startDate) ||
                !DateTime.TryParse(req.EndDate, out endDate))
            {
                return Fail("Please provide valid start and end dates.");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            int tripDays = (endDate - startDate).Days;

            if (tripDays < 1)
            {
                return Fail("End date must be after start date.");
            }

            if (tripDays > 30)
            {
                return Fail("Trip length cannot exceed 30 days.");
            }

            // Normalize dates to ISO format.
            req.StartDate = startDate.ToString("yyyy-MM-dd");
            req.EndDate = endDate.ToString("yyyy-MM-dd");

            int seconds = Math.Max(
                AiSettings.TimeoutSeconds,
                60
            );

            try
            {
                string body = JsonConvert.SerializeObject(req);

                using (var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(seconds)))
                using (var msg = new HttpRequestMessage(
                    HttpMethod.Post,
                    AiSettings.TourPlannerApiUrl))
                {
                    // Preserve legacy API authentication.
                    msg.Headers.TryAddWithoutValidation(
                        "Authorization",
                        "Bearer " + AiSettings.ApiKey
                    );

                    msg.Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json"
                    );

                    using (var resp = await _http.SendAsync(
                        msg,
                        cts.Token
                    ).ConfigureAwait(false))
                    {
                        return await HandleResponse(resp)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return Fail(
                    "The Tour Planner request timed out. Please try again."
                );
            }
            catch (HttpRequestException)
            {
                return Fail(
                    "Could not reach the AI service. Check your internet connection and try again."
                );
            }
            catch (JsonException)
            {
                return Fail(
                    "The Tour Planner request could not be serialized."
                );
            }
            catch (Exception)
            {
                return Fail(
                    "An unexpected error occurred while generating your itinerary."
                );
            }
        }

        // =====================================================
        // HANDLE API RESPONSE
        // =====================================================

        private async Task<TourPlannerServiceResult> HandleResponse(
            HttpResponseMessage resp)
        {
            string raw = await resp.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            int code = (int)resp.StatusCode;

            if (code == 401 || code == 403)
            {
                return Fail(
                    "Invalid or expired API key. Please check your configuration."
                );
            }

            if (code == 400 || code == 422)
            {
                return Fail(
                    "The AI service did not accept some of the trip details."
                );
            }

            if (code == 429)
            {
                return Fail(
                    "Rate limit reached. Please wait a moment and try again."
                );
            }

            if (code == 502 || code == 503 || code == 504)
            {
                return Fail(
                    "The AI service is temporarily unavailable. Please try again shortly."
                );
            }

            if (!resp.IsSuccessStatusCode)
            {
                return Fail(
                    "The AI service returned HTTP " + code + "."
                );
            }

            try
            {
                var data = JsonConvert.DeserializeObject<TourPlannerResponse>(
                    raw
                );

                if (data == null ||
                    data.Days == null ||
                    data.Days.Count == 0)
                {
                    return Fail(
                        "The AI returned an incomplete itinerary. Please try again."
                    );
                }

                return new TourPlannerServiceResult
                {
                    Success = true,
                    Data = data
                };
            }
            catch (JsonException)
            {
                return Fail(
                    "The AI response could not be read. Please try again."
                );
            }
        }

        // =====================================================
        // FAILURE RESULT
        // =====================================================

        private static TourPlannerServiceResult Fail(string msg)
        {
            return new TourPlannerServiceResult
            {
                Success = false,
                Error = msg
            };
        }

        // =====================================================
        // INPUT SANITIZATION
        // =====================================================

        private static string Sanitise(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            value = value.Trim();

            var builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (!char.IsControl(c) ||
                    c == '\n' ||
                    c == '\r')
                {
                    builder.Append(c);
                }
            }

            value = builder.ToString();

            return value.Length > maxLength
                ? value.Substring(0, maxLength)
                : value;
        }
    }
}
