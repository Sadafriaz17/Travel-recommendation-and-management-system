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
    PACKING ASSISTANT  —  JSON CONTRACT
    =========================================================

    REQUEST  (POST to AI:PackingApiUrl)
    {
      "destination":   "Paris, France",
      "days":          7,
      "startDate":     "2025-03-10",    // optional, ISO-8601
      "endDate":       "2025-03-17",    // optional, ISO-8601
      "tripType":      "honeymoon",     // adventure | family | business | honeymoon | religious | budget
      "travelers": [
        { "ageGroup": "adult",  "count": 2 },
        { "ageGroup": "child",  "count": 1 }
      ],
      "weather":       "cold, rainy",
      "specialNeeds":  "one traveler uses a cane"
    }

    RESPONSE  (expected from your AI model)
    {
      "weatherTip":  "Paris in March is cold — pack a waterproof layer.",
      "generalTips": ["Carry a power adapter", "Keep copies of all documents"],
      "categories": [
        {
          "name": "Clothes",
          "items": [
            { "name": "Warm jacket",   "quantity": "2", "priority": "Essential" },
            { "name": "Rain poncho",   "quantity": "1", "priority": "Recommended" },
            { "name": "Formal outfit", "quantity": "1", "priority": "Optional" }
          ]
        },
        {
          "name": "Documents",
          "items": [
            { "name": "Passport", "quantity": "1 per person", "priority": "Essential" }
          ]
        }
      ]
    }

    Priority values the model should use: "Essential" | "Recommended" | "Optional"
    Category names the model should use:
      Clothes | Documents | Electronics | Toiletries | Medicines | Accessories | Trip-Specific
    =========================================================
    */

    // ---- Request DTO ----
    public class PackingRequest
    {
        [JsonProperty("destination")]   public string Destination   { get; set; }
        [JsonProperty("days")]          public int    Days          { get; set; }
        [JsonProperty("startDate")]     public string StartDate     { get; set; }
        [JsonProperty("endDate")]       public string EndDate       { get; set; }
        [JsonProperty("tripType")]      public string TripType      { get; set; }
        [JsonProperty("travelers")]     public List<TravelerGroup> Travelers { get; set; }
        [JsonProperty("weather")]       public string Weather       { get; set; }
        [JsonProperty("specialNeeds")]  public string SpecialNeeds  { get; set; }
    }

    public class TravelerGroup
    {
        [JsonProperty("ageGroup")] public string AgeGroup { get; set; }  // adult | child | senior
        [JsonProperty("count")]    public int    Count    { get; set; }
    }

    // ---- Response DTOs ----
    public class PackingResponse
    {
        [JsonProperty("weatherTip")]    public string WeatherTip    { get; set; }
        [JsonProperty("generalTips")]   public List<string> GeneralTips { get; set; }
        [JsonProperty("categories")]    public List<PackingCategory> Categories { get; set; }
    }

    public class PackingCategory
    {
        [JsonProperty("name")]  public string Name  { get; set; }
        [JsonProperty("items")] public List<PackingItem> Items { get; set; }
    }

    public class PackingItem
    {
        [JsonProperty("name")]     public string Name     { get; set; }
        [JsonProperty("quantity")] public string Quantity { get; set; }
        [JsonProperty("priority")] public string Priority { get; set; }  // Essential | Recommended | Optional
    }

    // ---- Service result wrapper ----
    public class PackingServiceResult
    {
        public bool            Success  { get; set; }
        public string          Error    { get; set; }
        public PackingResponse Data     { get; set; }
    }

    // ---- Service ----
    public class PackingAssistantService
    {
        // Single shared HttpClient for the app lifetime
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>
        /// Validates the request, sends it to the AI API, and returns a typed result.
        /// The API key is added server-side and is never exposed to the browser.
        /// </summary>
        public async Task<PackingServiceResult> GenerateAsync(PackingRequest req)
        {
            // --- Pre-flight: are we configured? ---
            if (!AiSettings.IsPackingReady)
                return Fail("AI is not connected yet. Please add the API details in config.");

            // --- Sanitise text inputs ---
            req.Destination  = Sanitise(req.Destination,  200);
            req.Weather      = Sanitise(req.Weather,       200);
            req.SpecialNeeds = Sanitise(req.SpecialNeeds,  500);

            // --- Send request ---
            try
            {
                var body    = JsonConvert.SerializeObject(req);
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                using (var msg = new HttpRequestMessage(HttpMethod.Post, AiSettings.PackingApiUrl))
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

        private async Task<PackingServiceResult> HandleResponse(HttpResponseMessage resp)
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
                var data = JsonConvert.DeserializeObject<PackingResponse>(raw);
                if (data?.Categories == null || data.Categories.Count == 0)
                    return Fail("The AI returned an incomplete response. Please try again.");

                return new PackingServiceResult { Success = true, Data = data };
            }
            catch (JsonException)
            {
                return Fail("The AI response could not be read. Please try again.");
            }
        }

        private static PackingServiceResult Fail(string msg)
            => new PackingServiceResult { Success = false, Error = msg };

        /// <summary>Trims and caps a string. Removes control characters.</summary>
        private static string Sanitise(string s, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim();
            // Remove non-printable control chars (keep newlines for special needs field)
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                if (!char.IsControl(c) || c == '\n' || c == '\r') sb.Append(c);
            s = sb.ToString();
            return s.Length > maxLen ? s.Substring(0, maxLen) : s;
        }
    }
}
