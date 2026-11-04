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
    /// <summary>
    /// Reads AI tool configuration from appSettings.
    /// Paste your values into Web.secrets.config (which is excluded from source control).
    /// </summary>
    public static class AiSettings
    {
        public static string ApiKey => ConfigurationManager.AppSettings["AI:ApiKey"] ?? "";
        public static string PackingApiUrl => ConfigurationManager.AppSettings["AI:PackingApiUrl"] ?? "";
        public static string TourPlannerApiUrl => ConfigurationManager.AppSettings["AI:TourPlannerApiUrl"] ?? "";
        public static int TimeoutSeconds => int.TryParse(ConfigurationManager.AppSettings["AI:TimeoutSeconds"], out var t) ? t : 60;

        // NexVoy recommendation API (its own URL and key; the key is sent as X-API-Key, not as a Bearer token)
        public static string NexVoyApiUrl => (ConfigurationManager.AppSettings["AI:NexVoyApiUrl"] ?? "").Trim();
        public static string NexVoyApiKey => (ConfigurationManager.AppSettings["AI:NexVoyApiKey"] ?? "").Trim();

        /// <summary>Returns true only when both the key and the requested URL are non-empty.</summary>
        public static bool IsPackingReady => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(PackingApiUrl);
        public static bool IsTourReady => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(TourPlannerApiUrl);
        public static bool IsNexVoyReady => !string.IsNullOrWhiteSpace(NexVoyApiKey) && !string.IsNullOrWhiteSpace(NexVoyApiUrl);
    }

    /*
     * NEXVOY RECOMMENDATION API  -  JSON contract
     * -------------------------------------------
     * POST {AI:NexVoyApiUrl}/recommend      Header: X-API-Key: <AI:NexVoyApiKey>
     *
     * Request : { "temperature": 15, "budget": "Mid-range", "region": "europe" (or null),
     *             "preferences": { "culture":4, "adventure":3, "nature":5, "beaches":2, "nightlife":5,
     *                              "cuisine":5, "wellness":4, "urban":4, "seclusion":5 },
     *             "top_n": 5 }
     * Response: { "predicted_budget": "...", "selected_budget": "...", "notes": [...], "region_applied": "...",
     *             "results": [ { "rank", "city", "country", "region", "budget_level", "avg_hotel_usd",
     *                            "avg_temp", "ideal_trip_days", "ideal_trip_labels", "match_score",
     *                            "preference_match", "temperature_match", "budget_confidence", "description" } ] }
     */

    // ---- Request DTOs (server -> NexVoy API) ----
    public class NexVoyRequest
    {
        [JsonProperty("temperature")] public double Temperature { get; set; }
        [JsonProperty("budget")] public string Budget { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("preferences")] public NexVoyPreferences Preferences { get; set; }
        [JsonProperty("top_n")] public int TopN { get; set; }
    }

    public class NexVoyPreferences
    {
        [JsonProperty("culture")] public int Culture { get; set; }
        [JsonProperty("adventure")] public int Adventure { get; set; }
        [JsonProperty("nature")] public int Nature { get; set; }
        [JsonProperty("beaches")] public int Beaches { get; set; }
        [JsonProperty("nightlife")] public int Nightlife { get; set; }
        [JsonProperty("cuisine")] public int Cuisine { get; set; }
        [JsonProperty("wellness")] public int Wellness { get; set; }
        [JsonProperty("urban")] public int Urban { get; set; }
        [JsonProperty("seclusion")] public int Seclusion { get; set; }
    }

    // ---- Response DTOs (NexVoy API -> server) ----
    public class NexVoyResponse
    {
        [JsonProperty("model")] public string Model { get; set; }
        [JsonProperty("selected_budget")] public string SelectedBudget { get; set; }
        [JsonProperty("predicted_budget")] public string PredictedBudget { get; set; }
        [JsonProperty("predicted_budget_probabilities")] public Dictionary<string, double> PredictedBudgetProbabilities { get; set; }
        [JsonProperty("predicted_budget_basis")] public string PredictedBudgetBasis { get; set; }
        [JsonProperty("preferred_temperature")] public double PreferredTemperature { get; set; }
        [JsonProperty("region_applied")] public string RegionApplied { get; set; }
        [JsonProperty("notes")] public List<string> Notes { get; set; }
        [JsonProperty("count")] public int Count { get; set; }
        [JsonProperty("results")] public List<NexVoyItem> Results { get; set; }
    }

    public class NexVoyItem
    {
        [JsonProperty("rank")] public int Rank { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("budget_level")] public string BudgetLevel { get; set; }
        [JsonProperty("avg_hotel_usd")] public int? AvgHotelUsd { get; set; }
        [JsonProperty("avg_temp")] public double AvgTemp { get; set; }
        [JsonProperty("ideal_trip_days")] public string IdealTripDays { get; set; }
        [JsonProperty("ideal_trip_labels")] public string IdealTripLabels { get; set; }
        [JsonProperty("match_score")] public double MatchScore { get; set; }
        [JsonProperty("preference_match")] public double PreferenceMatch { get; set; }
        [JsonProperty("temperature_match")] public double TemperatureMatch { get; set; }
        [JsonProperty("budget_confidence")] public double BudgetConfidence { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
    }

    public class NexVoyServiceResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public NexVoyResponse Data { get; set; }
    }

    // ---- The service ----
    public class NexVoyService
    {
        // One shared HttpClient (avoids socket exhaustion). The timeout is applied per request below.
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;   // Render needs TLS 1.2+
            return new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        }

        public async Task<NexVoyServiceResult> RecommendAsync(NexVoyRequest request)
        {
            if (!AiSettings.IsNexVoyReady)
                return Fail("AI is not connected yet. Please add the API details in config.");

            // The free Render service sleeps when idle and can need about a minute to wake up.
            int seconds = Math.Max(AiSettings.TimeoutSeconds, 90);

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
                using (var msg = new HttpRequestMessage(HttpMethod.Post, AiSettings.NexVoyApiUrl.TrimEnd('/') + "/recommend"))
                {
                    msg.Headers.Add("X-API-Key", AiSettings.NexVoyApiKey);
                    msg.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                    using (var resp = await Http.SendAsync(msg, cts.Token).ConfigureAwait(false))
                    {
                        string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        int code = (int)resp.StatusCode;

                        if (code == 401 || code == 403) return Fail("Invalid or expired API key. Please check your configuration.");
                        if (code == 422) return Fail("Some of your answers were not accepted: " + ReadDetail(body));
                        if (code == 429) return Fail("Rate limit reached. Please wait a moment and try again.");
                        if (!resp.IsSuccessStatusCode) return Fail("The recommendation service returned an error (HTTP " + code + ").");

                        var data = JsonConvert.DeserializeObject<NexVoyResponse>(body);
                        if (data == null || data.Results == null)
                            return Fail("The recommendation service returned an incomplete response. Please try again.");

                        return new NexVoyServiceResult { Success = true, Data = data };
                    }
                }
            }
            catch (TaskCanceledException) { return Fail("The request timed out. Please try again."); }
            catch (HttpRequestException) { return Fail("Could not reach the AI service. Check your internet connection."); }
            catch (JsonException) { return Fail("The AI response could not be read. Please try again."); }
            catch (Exception) { return Fail("Something went wrong while getting recommendations."); }
        }

        // The API explains validation problems in "detail" (text, or a list of problems).
        private static string ReadDetail(string body)
        {
            try
            {
                var detail = JObject.Parse(body)["detail"];
                if (detail == null) return "invalid input.";
                if (detail.Type == JTokenType.String) return detail.ToString();
                var first = detail.First != null ? detail.First["msg"] : null;
                return first != null ? first.ToString() : "invalid input.";
            }
            catch { return "invalid input."; }
        }

        private static NexVoyServiceResult Fail(string message)
        {
            return new NexVoyServiceResult { Success = false, Error = message };
        }
    }
}
