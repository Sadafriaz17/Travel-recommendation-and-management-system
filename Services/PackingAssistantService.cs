using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TourwebsiteFYP.Services
{
    /*
    =========================================================
    NEW NEXVOY PACKING ASSISTANT API

    POST {AI:PackAssistantApiUrl}/pack
    Header: X-API-Key

    Request:
    {
        destination: {...},
        trip: {...},
        planner: {...},
        options: {...}
    }

    Response (documented shape - an OBJECT):
    {
        destination, trip, weather, model_inputs_used,
        ml_items, essentials, extra_weather_suggestions,
        groups, counts, notes, limitations, model
    }

    This service NEVER accepts an array body as a valid response.
    =========================================================
    */

    public class PackingRequest
    {
        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("days")]
        public int Days { get; set; }

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("endDate")]
        public string EndDate { get; set; }

        [JsonProperty("tripType")]
        public string TripType { get; set; }

        [JsonProperty("travelers")]
        public List<TravelerGroup> Travelers { get; set; }

        [JsonProperty("weather")]
        public string Weather { get; set; }

        [JsonProperty("specialNeeds")]
        public string SpecialNeeds { get; set; }
    }

    public class TravelerGroup
    {
        [JsonProperty("ageGroup")]
        public string AgeGroup { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class PackingResponse
    {
        [JsonProperty("weatherTip")]
        public string WeatherTip { get; set; }

        [JsonProperty("generalTips")]
        public List<string> GeneralTips { get; set; }

        [JsonProperty("categories")]
        public List<PackingCategory> Categories { get; set; }
    }

    public class PackingCategory
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("items")]
        public List<PackingItem> Items { get; set; }
    }

    public class PackingItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }

        [JsonProperty("priority")]
        public string Priority { get; set; }
    }

    public class PackingServiceResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public PackingResponse Data { get; set; }
    }

    public class PackAssistantResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public JObject Data { get; set; }
    }

    public class PackingAssistantService
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            return new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public async Task<PackAssistantResult> PackAsync(JObject request)
        {
            if (!AiSettings.IsPackAssistantReady)
                return PackFail("The new Packing Assistant is not connected. Please configure its URL and API key in Web.secrets.config.");

            if (request == null)
                return PackFail("Packing request is empty.");

            if (request["destination"] == null || request["trip"] == null)
                return PackFail("Packing request must include destination and trip details.");

            int seconds = Math.Max(AiSettings.TimeoutSeconds, 90);

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
                using (var msg = new HttpRequestMessage(
                    HttpMethod.Post,
                    AiSettings.PackAssistantApiUrl.TrimEnd('/') + "/pack"))
                {
                    msg.Headers.Add("X-API-Key", AiSettings.PackAssistantApiKey);

                    msg.Content = new StringContent(
                        request.ToString(Formatting.None),
                        Encoding.UTF8,
                        "application/json"
                    );

                    using (var resp = await _http.SendAsync(msg, cts.Token).ConfigureAwait(false))
                    {
                        string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        int code = (int)resp.StatusCode;

                        if (code == 401 || code == 403)
                            return PackFail("Invalid or expired Packing Assistant API key. Please check Web.secrets.config.");

                        if (code == 400 || code == 422)
                            return PackFail("The Packing Assistant did not accept the supplied trip details: " + ReadPackDetail(body));

                        if (code == 429)
                            return PackFail("Packing API rate limit reached. Please wait and try again.");

                        if (code == 502 || code == 503 || code == 504)
                            return PackFail("The Packing Assistant service is waking up or temporarily unavailable. Please try again in a minute.");

                        if (!resp.IsSuccessStatusCode)
                            return PackFail("The Packing Assistant returned HTTP " + code + ".");

                        if (string.IsNullOrWhiteSpace(body))
                            return PackFail("The Packing Assistant returned an empty body.");

                        string trimmed = body.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

                        if (trimmed.Length == 0)
                            return PackFail("The Packing Assistant returned an empty body.");

                        if (trimmed[0] == '[')
                            return PackFail("The Packing Assistant returned a JSON array instead of a JSON object. This usually means the /pack endpoint is misconfigured on the API server.");

                        if (trimmed[0] != '{')
                            return PackFail("The Packing Assistant returned an unexpected response (not JSON).");

                        JObject data;
                        try
                        {
                            data = JObject.Parse(body);
                        }
                        catch (JsonException ex)
                        {
                            return PackFail("The Packing Assistant returned invalid JSON: " + ex.Message);
                        }

                        if (data == null)
                            return PackFail("The Packing Assistant returned no data.");

                        JToken groupsToken = data["groups"];
                        JToken mlToken = data["ml_items"];
                        JToken essToken = data["essentials"];
                        JToken extraToken = data["extra_weather_suggestions"];

                        if (groupsToken != null && !(groupsToken is JArray))
                            return PackFail("The Packing Assistant response has a non-array 'groups' field.");

                        if (mlToken != null && !(mlToken is JArray))
                            return PackFail("The Packing Assistant response has a non-array 'ml_items' field.");

                        if (essToken != null && !(essToken is JArray))
                            return PackFail("The Packing Assistant response has a non-array 'essentials' field.");

                        if (extraToken != null && !(extraToken is JArray))
                            return PackFail("The Packing Assistant response has a non-array 'extra_weather_suggestions' field.");

                        bool hasGroups = groupsToken is JArray groupsArr && groupsArr.Count > 0;
                        bool hasMl = mlToken is JArray mlArr && mlArr.Count > 0;
                        bool hasEss = essToken is JArray essArr && essArr.Count > 0;
                        bool hasExtra = extraToken is JArray extraArr && extraArr.Count > 0;

                        if (!hasGroups && !hasMl && !hasEss && !hasExtra)
                            return PackFail("The Packing Assistant returned no packing items for this destination and trip. Please check the city and country, or try different trip dates.");

                        return new PackAssistantResult { Success = true, Data = data };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return PackFail("Packing request timed out. The service may be waking up. Please try again.");
            }
            catch (HttpRequestException)
            {
                return PackFail("Could not reach the new Packing Assistant. Check your internet connection.");
            }
            catch (JsonException)
            {
                return PackFail("The Packing Assistant response could not be read.");
            }
            catch (Exception)
            {
                return PackFail("Something went wrong while generating your packing list.");
            }
        }

        private static string ReadPackDetail(string body)
        {
            try
            {
                var detail = JObject.Parse(body)["detail"];
                if (detail == null) return "Invalid input.";
                if (detail.Type == JTokenType.String) return detail.ToString();

                if (detail is JArray)
                {
                    var first = detail.First;
                    var message = first != null ? first["msg"] : null;
                    return message != null ? message.ToString() : "Invalid input.";
                }
                return "Invalid input.";
            }
            catch
            {
                return "Invalid input.";
            }
        }

        private static PackAssistantResult PackFail(string message)
            => new PackAssistantResult { Success = false, Error = message };

        public async Task<PackingServiceResult> GenerateAsync(PackingRequest req)
        {
            if (!AiSettings.IsPackingReady)
                return Fail("The legacy Packing Assistant is not connected.");

            if (req == null) return Fail("Invalid packing request.");

            req.Destination = Sanitise(req.Destination, 200);
            req.Weather = Sanitise(req.Weather, 200);
            req.SpecialNeeds = Sanitise(req.SpecialNeeds, 500);

            if (string.IsNullOrWhiteSpace(req.Destination))
                return Fail("Please enter a destination.");

            int seconds = Math.Max(AiSettings.TimeoutSeconds, 30);

            try
            {
                string body = JsonConvert.SerializeObject(req);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
                using (var msg = new HttpRequestMessage(HttpMethod.Post, AiSettings.PackingApiUrl))
                {
                    msg.Headers.TryAddWithoutValidation("Authorization", "Bearer " + AiSettings.ApiKey);
                    msg.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    using (var resp = await _http.SendAsync(msg, cts.Token).ConfigureAwait(false))
                        return await HandleResponse(resp).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return Fail("The request timed out. Please try again.");
            }
            catch (HttpRequestException)
            {
                return Fail("Could not reach the legacy AI service. Check your internet connection.");
            }
            catch (Exception)
            {
                return Fail("An unexpected error occurred while generating the packing list.");
            }
        }

        private async Task<PackingServiceResult> HandleResponse(HttpResponseMessage resp)
        {
            string raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.Unauthorized ||
                resp.StatusCode == HttpStatusCode.Forbidden)
                return Fail("Invalid or expired legacy AI API key.");

            if ((int)resp.StatusCode == 429)
                return Fail("Rate limit reached. Please wait and try again.");

            if (!resp.IsSuccessStatusCode)
                return Fail("The legacy AI service returned HTTP " + (int)resp.StatusCode + ".");

            try
            {
                var data = JsonConvert.DeserializeObject<PackingResponse>(raw);

                if (data == null || data.Categories == null || data.Categories.Count == 0)
                    return Fail("The legacy AI returned an incomplete packing list.");

                return new PackingServiceResult { Success = true, Data = data };
            }
            catch (JsonException)
            {
                return Fail("The legacy AI response could not be read.");
            }
        }

        private static PackingServiceResult Fail(string message)
            => new PackingServiceResult { Success = false, Error = message };

        private static string Sanitise(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim();

            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (!char.IsControl(c) || c == '\n' || c == '\r') builder.Append(c);
            }

            value = builder.ToString();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }
    }
}