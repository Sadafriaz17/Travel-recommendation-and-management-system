
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TourwebsiteFYP.Services
{
    public static class AiSettings
    {
        // Legacy AI settings - preserved for existing tools
        public static string ApiKey =>
            ConfigurationManager.AppSettings["AI:ApiKey"] ?? "";

        public static string PackingApiUrl =>
            ConfigurationManager.AppSettings["AI:PackingApiUrl"] ?? "";

        public static string TourPlannerApiUrl =>
            ConfigurationManager.AppSettings["AI:TourPlannerApiUrl"] ?? "";

        public static int TimeoutSeconds =>
            int.TryParse(ConfigurationManager.AppSettings["AI:TimeoutSeconds"], out var t)
                ? Math.Max(t, 60)
                : 90;

        // Existing Tour Recommendation API
        public static string NexVoyApiUrl =>
            (ConfigurationManager.AppSettings["AI:NexVoyApiUrl"] ?? "").Trim();

        public static string NexVoyApiKey =>
            (ConfigurationManager.AppSettings["AI:NexVoyApiKey"] ?? "").Trim();

        // New Packing Assistant API
        public static string PackAssistantApiUrl =>
            (ConfigurationManager.AppSettings["AI:PackAssistantApiUrl"] ?? "").Trim();

        public static string PackAssistantApiKey =>
            (ConfigurationManager.AppSettings["AI:PackAssistantApiKey"] ?? "").Trim();

        // Readiness checks
        public static bool IsPackingReady =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(PackingApiUrl);

        public static bool IsTourReady =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(TourPlannerApiUrl);

        public static bool IsNexVoyReady =>
            !string.IsNullOrWhiteSpace(NexVoyApiKey) &&
            !string.IsNullOrWhiteSpace(NexVoyApiUrl);

        public static bool IsPackAssistantReady =>
            !string.IsNullOrWhiteSpace(PackAssistantApiKey) &&
            !string.IsNullOrWhiteSpace(PackAssistantApiUrl);
    }

    // ============================================================
    // NEXVOY TOUR RECOMMENDATION API
    // POST /recommend
    // Header: X-API-Key
    // ============================================================

    public class NexVoyRequest
    {
        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("budget")]
        public string Budget { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("preferences")]
        public NexVoyPreferences Preferences { get; set; }

        [JsonProperty("top_n")]
        public int TopN { get; set; }

        [JsonProperty("travel_month", NullValueHandling = NullValueHandling.Ignore)]
        public int? TravelMonth { get; set; }

        [JsonProperty("travel_start_date", NullValueHandling = NullValueHandling.Ignore)]
        public string TravelStartDate { get; set; }

        [JsonProperty("travel_end_date", NullValueHandling = NullValueHandling.Ignore)]
        public string TravelEndDate { get; set; }
    }

    public class NexVoyPreferences
    {
        [JsonProperty("culture")]
        public int Culture { get; set; }

        [JsonProperty("adventure")]
        public int Adventure { get; set; }

        [JsonProperty("nature")]
        public int Nature { get; set; }

        [JsonProperty("beaches")]
        public int Beaches { get; set; }

        [JsonProperty("nightlife")]
        public int Nightlife { get; set; }

        [JsonProperty("cuisine")]
        public int Cuisine { get; set; }

        [JsonProperty("wellness")]
        public int Wellness { get; set; }

        [JsonProperty("urban")]
        public int Urban { get; set; }

        [JsonProperty("seclusion")]
        public int Seclusion { get; set; }
    }

    // ============================================================
    // TOUR RECOMMENDATION RESPONSE
    // ============================================================

    public class NexVoyResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("selected_budget")]
        public string SelectedBudget { get; set; }

        [JsonProperty("predicted_budget")]
        public string PredictedBudget { get; set; }

        [JsonProperty("predicted_budget_probabilities")]
        public Dictionary<string, double> PredictedBudgetProbabilities { get; set; }

        [JsonProperty("predicted_budget_basis")]
        public string PredictedBudgetBasis { get; set; }

        [JsonProperty("preferred_temperature")]
        public double PreferredTemperature { get; set; }

        [JsonProperty("region_applied")]
        public string RegionApplied { get; set; }

        [JsonProperty("notes")]
        public List<string> Notes { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("results")]
        public List<NexVoyItem> Results { get; set; }

        [JsonProperty("weather_source")]
        public string WeatherSource { get; set; }

        [JsonProperty("forecast_candidates_evaluated")]
        public int? ForecastCandidatesEvaluated { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    public class NexVoyItem
    {
        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("budget_level")]
        public string BudgetLevel { get; set; }

        [JsonProperty("avg_hotel_usd")]
        public int? AvgHotelUsd { get; set; }

        [JsonProperty("avg_temp")]
        public double AvgTemp { get; set; }

        [JsonProperty("ideal_trip_days")]
        public string IdealTripDays { get; set; }

        [JsonProperty("ideal_trip_labels")]
        public string IdealTripLabels { get; set; }

        [JsonProperty("match_score")]
        public double MatchScore { get; set; }

        [JsonProperty("preference_match")]
        public double PreferenceMatch { get; set; }

        [JsonProperty("temperature_match")]
        public double TemperatureMatch { get; set; }

        [JsonProperty("budget_confidence")]
        public double BudgetConfidence { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("forecast_avg_temp")]
        public double? ForecastAvgTemp { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    public class NexVoyServiceResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public NexVoyResponse Data { get; set; }
    }

    // ============================================================
    // TOUR RECOMMENDATION SERVICE
    // Existing deployed recommendation model remains unchanged.
    // ============================================================

    public class NexVoyService
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            return new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public async Task<NexVoyServiceResult> RecommendAsync(
            NexVoyRequest request)
        {
            if (!AiSettings.IsNexVoyReady)
            {
                return Fail(
                    "Tour Recommendation AI is not connected. Please check its API configuration."
                );
            }

            if (request == null)
            {
                return Fail("Invalid recommendation request.");
            }

            if (request.Preferences == null)
            {
                return Fail("Please provide your travel preferences.");
            }

            if (string.IsNullOrWhiteSpace(request.TravelStartDate) !=
                string.IsNullOrWhiteSpace(request.TravelEndDate))
            {
                return Fail("Please provide both travel dates, or leave both empty.");
            }

            int seconds = Math.Max(AiSettings.TimeoutSeconds, 90);

            try
            {
                using (var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(seconds)))
                using (var msg = new HttpRequestMessage(
                    HttpMethod.Post,
                    AiSettings.NexVoyApiUrl.TrimEnd('/') + "/recommend"))
                {
                    msg.Headers.Add("X-API-Key", AiSettings.NexVoyApiKey);

                    msg.Content = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );

                    using (var resp = await Http.SendAsync(msg, cts.Token)
                        .ConfigureAwait(false))
                    {
                        string body = await resp.Content.ReadAsStringAsync()
                            .ConfigureAwait(false);

                        int code = (int)resp.StatusCode;

                        if (code == 401 || code == 403)
                            return Fail("Invalid or expired Tour Recommendation API key.");

                        if (code == 422 || code == 400)
                            return Fail("Some answers were not accepted: " + ReadDetail(body));

                        if (code == 429)
                            return Fail("Recommendation API rate limit reached. Please try again shortly.");

                        if (code == 502 || code == 503 || code == 504)
                            return Fail("Recommendation service is waking up or temporarily unavailable. Please try again in a minute.");

                        if (!resp.IsSuccessStatusCode)
                            return Fail("Recommendation service returned HTTP " + code + ".");

                        var data = JsonConvert.DeserializeObject<NexVoyResponse>(body);

                        if (data == null || data.Results == null)
                        {
                            return Fail(
                                "Recommendation service returned an incomplete response."
                            );
                        }

                        return new NexVoyServiceResult
                        {
                            Success = true,
                            Data = data
                        };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return Fail("Recommendation request timed out. Please try again.");
            }
            catch (HttpRequestException)
            {
                return Fail("Could not reach the Tour Recommendation service. Check your internet connection.");
            }
            catch (JsonException)
            {
                return Fail("The recommendation response could not be read.");
            }
            catch (Exception)
            {
                return Fail("Something went wrong while getting recommendations.");
            }
        }

        private static string ReadDetail(string body)
        {
            try
            {
                var detail = JObject.Parse(body)["detail"];

                if (detail == null)
                    return "Invalid input.";

                if (detail.Type == JTokenType.String)
                    return detail.ToString();

                var first = detail.First;
                var message = first != null ? first["msg"] : null;

                return message != null
                    ? message.ToString()
                    : "Invalid input.";
            }
            catch
            {
                return "Invalid input.";
            }
        }

        private static NexVoyServiceResult Fail(string message)
        {
            return new NexVoyServiceResult
            {
                Success = false,
                Error = message
            };
        }
    }
}
