using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TourwebsiteFYP.Services
{
    /*
    =========================================================
    TOUR PLANNER  —  JSON CONTRACT
    =========================================================

    REQUEST  (POST to AI:TourPlannerApiUrl)
    {
      "fromCity":        "Lahore",
      "destination":     "Istanbul, Turkey",
      "startDate":       "2025-04-01",   // ISO-8601
      "endDate":         "2025-04-07",   // ISO-8601
      "budgetMin":       1500,
      "budgetMax":       3000,
      "currency":        "USD",
      "travelers":       2,
      "interests":       ["culture", "food", "history"],
      "travelStyle":     "balanced",     // relaxed | balanced | packed
      "healthNotes":     "one traveler has a knee injury, avoid steep hikes"
    }

    RESPONSE  (expected from your AI model)
    {
      "totalBudget": {
        "estimated": 2400,
        "currency":  "USD",
        "breakdown": {
          "accommodation": 900,
          "food":          480,
          "transport":     320,
          "activities":    400,
          "misc":          300
        }
      },
      "healthNotes": "Avoided steep hills; all venues wheelchair-accessible.",
      "days": [
        {
          "dayNumber": 1,
          "date":      "2025-04-01",
          "title":     "Arrival & Old City",
          "slots": [
            {
              "period":      "Morning",
              "time":        "09:00",
              "place":       "Sultanahmet Square",
              "description": "Arrive, settle in, and walk the historic square.",
              "estimatedCost": 0
            },
            {
              "period":      "Afternoon",
              "time":        "13:00",
              "place":       "Hagia Sophia",
              "description": "Explore the iconic cathedral-mosque.",
              "estimatedCost": 25
            },
            {
              "period":      "Evening",
              "time":        "19:00",
              "place":       "Karaköy waterfront",
              "description": "Dinner with Bosphorus views.",
              "estimatedCost": 35
            }
          ]
        }
      ]
    }

    Period values the model should use: "Morning" | "Afternoon" | "Evening"
    travelStyle values:  "relaxed" (2 activities/day) | "balanced" (3-4) | "packed" (5+)
    =========================================================
    */

    // ---- Request DTO ----
    public class TourPlannerRequest
    {
        [JsonProperty("fromCity")]     public string       FromCity     { get; set; }
        [JsonProperty("destination")]  public string       Destination  { get; set; }
        [JsonProperty("startDate")]    public string       StartDate    { get; set; }
        [JsonProperty("endDate")]      public string       EndDate      { get; set; }
        [JsonProperty("budgetMin")]    public decimal      BudgetMin    { get; set; }
        [JsonProperty("budgetMax")]    public decimal      BudgetMax    { get; set; }
        [JsonProperty("currency")]     public string       Currency     { get; set; }
        [JsonProperty("travelers")]    public int          Travelers    { get; set; }
        [JsonProperty("interests")]    public List<string> Interests    { get; set; }
        [JsonProperty("travelStyle")]  public string       TravelStyle  { get; set; }
        [JsonProperty("healthNotes")]  public string       HealthNotes  { get; set; }
    }

    // ---- Response DTOs ----
    public class TourPlannerResponse
    {
        [JsonProperty("totalBudget")]  public BudgetSummary   TotalBudget  { get; set; }
        [JsonProperty("healthNotes")]  public string          HealthNotes  { get; set; }
        [JsonProperty("days")]         public List<TourDay>   Days         { get; set; }
    }

    public class BudgetSummary
    {
        [JsonProperty("estimated")]    public decimal          Estimated  { get; set; }
        [JsonProperty("currency")]     public string           Currency   { get; set; }
        [JsonProperty("breakdown")]    public BudgetBreakdown  Breakdown  { get; set; }
    }

    public class BudgetBreakdown
    {
        [JsonProperty("accommodation")] public decimal Accommodation { get; set; }
        [JsonProperty("food")]          public decimal Food          { get; set; }
        [JsonProperty("transport")]     public decimal Transport     { get; set; }
        [JsonProperty("activities")]    public decimal Activities    { get; set; }
        [JsonProperty("misc")]          public decimal Misc          { get; set; }
    }

    public class TourDay
    {
        [JsonProperty("dayNumber")]  public int           DayNumber  { get; set; }
        [JsonProperty("date")]       public string        Date       { get; set; }
        [JsonProperty("title")]      public string        Title      { get; set; }
        [JsonProperty("slots")]      public List<TourSlot> Slots     { get; set; }
    }

    public class TourSlot
    {
        [JsonProperty("period")]        public string  Period        { get; set; }   // Morning|Afternoon|Evening
        [JsonProperty("time")]          public string  Time          { get; set; }
        [JsonProperty("place")]         public string  Place         { get; set; }
        [JsonProperty("description")]   public string  Description   { get; set; }
        [JsonProperty("estimatedCost")] public decimal EstimatedCost { get; set; }
    }

    // ---- Service result wrapper ----
    public class TourPlannerServiceResult
    {
        public bool                 Success  { get; set; }
        public string               Error    { get; set; }
        public TourPlannerResponse  Data     { get; set; }
    }

    // ---- Service ----
    public class TourPlannerService
    {
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>
        /// Validates the request, calls the AI API, and returns a typed result.
        /// The API key lives only on the server and is never sent to the browser.
        /// </summary>
        public async Task<TourPlannerServiceResult> GenerateAsync(TourPlannerRequest req)
        {
            if (!AiSettings.IsTourReady)
                return Fail("AI is not connected yet. Please add the API details in config.");

            // Sanitise free-text fields
            req.FromCity    = Sanitise(req.FromCity,    200);
            req.Destination = Sanitise(req.Destination, 200);
            req.HealthNotes = Sanitise(req.HealthNotes, 500);
            req.Currency    = Sanitise(req.Currency,      5);

            // Cap trip length to 30 days
            if (DateTime.TryParse(req.StartDate, out var sd) && DateTime.TryParse(req.EndDate, out var ed))
            {
                var days = (ed - sd).Days;
                if (days < 1)  return Fail("End date must be after start date.");
                if (days > 30) return Fail("Trip length cannot exceed 30 days.");
            }

            try
            {
                var body    = JsonConvert.SerializeObject(req);
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                using (var msg = new HttpRequestMessage(HttpMethod.Post, AiSettings.TourPlannerApiUrl))
                {
                    msg.Headers.TryAddWithoutValidation("Authorization", "Bearer " + AiSettings.ApiKey);
                    msg.Content = content;

                    _http.Timeout = TimeSpan.FromSeconds(AiSettings.TimeoutSeconds);
                    var resp = await _http.SendAsync(msg).ConfigureAwait(false);

                    return await HandleResponse(resp).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                return Fail("The request timed out. Please try again.");
            }
            catch (HttpRequestException)
            {
                return Fail("Could not reach the AI service. Check your internet connection and try again.");
            }
            catch (Exception ex)
            {
                return Fail("An unexpected error occurred: " + ex.Message);
            }
        }

        private async Task<TourPlannerServiceResult> HandleResponse(HttpResponseMessage resp)
        {
            var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
                return Fail("Invalid or expired API key. Please check your configuration.");

            if ((int)resp.StatusCode == 429)
                return Fail("Rate limit reached. Please wait a moment and try again.");

            if (!resp.IsSuccessStatusCode)
                return Fail($"The AI service returned an error (HTTP {(int)resp.StatusCode}).");

            try
            {
                var data = JsonConvert.DeserializeObject<TourPlannerResponse>(raw);
                if (data?.Days == null || data.Days.Count == 0)
                    return Fail("The AI returned an incomplete response. Please try again.");

                return new TourPlannerServiceResult { Success = true, Data = data };
            }
            catch (JsonException)
            {
                return Fail("The AI response could not be read. Please try again.");
            }
        }

        private static TourPlannerServiceResult Fail(string msg)
            => new TourPlannerServiceResult { Success = false, Error = msg };

        private static string Sanitise(string s, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim();
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                if (!char.IsControl(c) || c == '\n' || c == '\r') sb.Append(c);
            s = sb.ToString();
            return s.Length > maxLen ? s.Substring(0, maxLen) : s;
        }
    }
}
