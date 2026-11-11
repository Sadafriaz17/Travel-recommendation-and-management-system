using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class PackingAssistantController : Controller
    {
        private const string BuildTag = "PackingAssistant/2026-11-09-json-fix";

        // Set to false once the fix is confirmed.
        private const bool VerboseLogging = true;

        private readonly PackingAssistantService _service =
            new PackingAssistantService();

        private static readonly string[] AgeGroups =
            { "Child", "Teen", "Young Adult", "Adult", "Senior" };

        private static readonly string[] PackingPreferences =
            { "Balanced", "Budget", "Comprehensive", "Eco-Friendly", "Luxury", "Minimalist" };

        private static readonly string[] Transportations =
            { "airplane", "bus", "car", "cruise", "train" };

        private static readonly string[] Accommodations =
            { "airbnb", "camping", "guesthouse", "homestay", "hostel", "hotel", "resort" };

        private static readonly string[] TripTypes =
        {
            "Adventure","Backpacking","Beach Vacation","Business",
            "Business Conference","Couple","Cultural","Educational",
            "Family","Festival Trip","Hiking Trip","Honeymoon",
            "Leisure","Religious","Road Trip","Ski Trip","Solo",
            "Sports Trip","Volunteer Trip","Wellness"
        };

        private static readonly string[] Activities =
        {
            "Adventure Sports","Backpacking","Beach","Business Meetings",
            "Camping","Cultural Events","Cycling","Desert Safari","Dining",
            "Family Visit","Festivals","Fitness","Hiking","Museum Visits",
            "Nightlife","Photography","Religious Visits","Remote Work",
            "Road Trip","Safari","Shopping","Sightseeing","Skiing",
            "Snowboarding","Sports Event","Swimming","Trekking",
            "Volunteering","Water Sports","Wellness"
        };

        private static readonly string[] ClimateZones =
        {
            "arid","continental","desert","highland","mediterranean",
            "oceanic","semi_arid","subarctic","subtropical","temperate",
            "tropical","tropical_monsoon"
        };

        private static readonly string[] Budgets = { "Budget", "Mid-range", "Luxury" };

        private static readonly string[] Regions =
        {
            "africa","asia","europe","middle_east",
            "north_america","oceania","south_america"
        };

        // The packing model supports trips up to 30 days.
        private const int MaxTripDays = 30;

        public ActionResult Index()
        {
            Response.AddHeader("X-NexVoy-Build", BuildTag);

            ViewBag.IsReady = AiSettings.IsPackAssistantReady;

            PackingPrefill prefill = TempData["PackingPrefill"] as PackingPrefill;

            if (prefill == null)
            {
                string qCity = (Request.QueryString["city"] ?? "").Trim();
                string qCountry = (Request.QueryString["country"] ?? "").Trim();
                string qStart = (Request.QueryString["start"] ?? "").Trim();
                string qEnd = (Request.QueryString["end"] ?? "").Trim();
                int qDays = 0;
                int.TryParse(Request.QueryString["days"], out qDays);

                if (qCity.Length > 0 || qCountry.Length > 0)
                {
                    prefill = new PackingPrefill
                    {
                        City = qCity,
                        Country = qCountry,
                        StartDate = qStart,
                        EndDate = qEnd,
                        Days = qDays
                    };
                }
            }

            if (prefill != null)
            {
                ViewBag.FromRecommender = "1";
                ViewBag.PrefillCity = prefill.City ?? "";
                ViewBag.PrefillCountry = prefill.Country ?? "";
                ViewBag.PrefillStart = prefill.StartDate ?? "";
                ViewBag.PrefillEnd = prefill.EndDate ?? "";
                ViewBag.PrefillDays = prefill.Days;
                ViewBag.PrefillTemperature = prefill.Temperature;
                ViewBag.PrefillBudget = prefill.Budget ?? "";
                ViewBag.PrefillRegion = prefill.Region ?? "";
                ViewBag.PrefillPrefs = prefill.Prefs ?? new Dictionary<string, int>();
            }
            else
            {
                ViewBag.FromRecommender = null;
                ViewBag.PrefillCity = "";
                ViewBag.PrefillCountry = "";
                ViewBag.PrefillStart = "";
                ViewBag.PrefillEnd = "";
                ViewBag.PrefillDays = 0;
                ViewBag.PrefillTemperature = null;
                ViewBag.PrefillBudget = "";
                ViewBag.PrefillRegion = "";
                ViewBag.PrefillPrefs = new Dictionary<string, int>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Generate(PackingFormModel form)
        {
            Response.AddHeader("X-NexVoy-Build", BuildTag);

            try
            {
                if (form == null)
                    return Reply(false, "Invalid packing request.");

                string city = NullIfBlank(form.City);
                string country = NullIfBlank(form.Country);

                if (city == null && !string.IsNullOrWhiteSpace(form.Destination))
                {
                    var parts = form.Destination.Split(',');
                    if (parts.Length >= 2)
                    {
                        city = parts[0].Trim();
                        country = parts[parts.Length - 1].Trim();
                    }
                    else
                    {
                        city = form.Destination.Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(city))
                    return Reply(false, "City or destination is required.");

                if (string.IsNullOrWhiteSpace(country))
                    return Reply(false, "Country is required.");

                DateTime startDate;
                DateTime endDate;

                if (string.IsNullOrWhiteSpace(form.StartDate) ||
                    string.IsNullOrWhiteSpace(form.EndDate))
                    return Reply(false, "Both trip start and end dates are required.");

                if (!DateTime.TryParseExact(form.StartDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                    return Reply(false, "Invalid trip start date.");

                if (!DateTime.TryParseExact(form.EndDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                    return Reply(false, "Invalid trip end date.");

                if (startDate.Date < DateTime.Today)
                    return Reply(false, "Trip start date cannot be in the past.");

                if (endDate < startDate)
                    return Reply(false, "End date cannot be earlier than start date.");

                int totalDays = (endDate - startDate).Days + 1;
                if (totalDays > MaxTripDays)
                    return Reply(false, "Trips longer than " + MaxTripDays + " days are not supported.");

                int totalTravelers = form.Adults + form.Children + form.Seniors;
                if (totalTravelers < 1)
                    return Reply(false, "Please select at least one traveler.");

                if (string.IsNullOrWhiteSpace(form.TripType) ||
                    !TripTypes.Contains(form.TripType.Trim()))
                    return Reply(false, "Please choose a valid trip type.");

                string ageGroup = NullIfBlank(form.AgeGroup);
                if (ageGroup != null && !AgeGroups.Contains(ageGroup))
                    return Reply(false, "Unknown age group.");

                string packingPref = NullIfBlank(form.PackingPreference);
                if (packingPref != null && !PackingPreferences.Contains(packingPref))
                    return Reply(false, "Unknown packing preference.");

                string transportation = NullIfBlank(form.Transportation);
                if (transportation != null && !Transportations.Contains(transportation))
                    return Reply(false, "Unknown transportation.");

                string accommodation = NullIfBlank(form.Accommodation);
                if (accommodation != null && !Accommodations.Contains(accommodation))
                    return Reply(false, "Unknown accommodation.");

                string climateZone = NullIfBlank(form.ClimateZone)?.ToLowerInvariant();
                if (climateZone != null && !ClimateZones.Contains(climateZone))
                    return Reply(false, "Unknown climate zone.");

                var activities = new JArray();
                if (!string.IsNullOrWhiteSpace(form.Activities))
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var raw in form.Activities.Split(','))
                    {
                        var name = raw.Trim();
                        if (name.Length == 0) continue;

                        var canonical = Activities.FirstOrDefault(act =>
                            string.Equals(act, name, StringComparison.OrdinalIgnoreCase));

                        if (canonical == null)
                            return Reply(false, "Unknown activity: " + name);

                        if (seen.Add(canonical)) activities.Add(canonical);
                    }
                }

                double preferredTemperature = form.Temperature ?? 22.0;
                if (preferredTemperature < -50 || preferredTemperature > 50)
                    return Reply(false, "Preferred temperature must be between -50 and 50 °C.");

                string budget = NullIfBlank(form.Budget);
                if (budget != null && !Budgets.Contains(budget))
                    return Reply(false, "Unknown budget.");

                string region = NullIfBlank(form.Region)?.ToLowerInvariant();
                if (region != null && !Regions.Contains(region))
                    return Reply(false, "Unknown region.");

                var plannerPrefs = new JObject
                {
                    ["culture"] = Clamp(form.Culture, 3),
                    ["adventure"] = Clamp(form.Adventure, 3),
                    ["nature"] = Clamp(form.Nature, 3),
                    ["beaches"] = Clamp(form.Beaches, 3),
                    ["nightlife"] = Clamp(form.Nightlife, 3),
                    ["cuisine"] = Clamp(form.Cuisine, 3),
                    ["wellness"] = Clamp(form.Wellness, 3),
                    ["urban"] = Clamp(form.Urban, 3),
                    ["seclusion"] = Clamp(form.Seclusion, 3)
                };

                var destObj = new JObject { ["city"] = city, ["country"] = country };

                var planner = new JObject
                {
                    ["preferred_temperature_c"] = preferredTemperature,
                    ["preferences"] = plannerPrefs
                };
                if (budget != null) planner["budget"] = budget;
                if (region != null) planner["region"] = region;

                var options = new JObject { ["trip_type"] = form.TripType.Trim() };
                if (ageGroup != null) options["age_group"] = ageGroup;
                if (packingPref != null) options["packing_preference"] = packingPref;
                if (form.LaundryAvailable.HasValue) options["laundry_available"] = form.LaundryAvailable.Value;
                if (transportation != null) options["transportation"] = transportation;
                if (accommodation != null) options["accommodation"] = accommodation;
                if (climateZone != null) options["climate_zone"] = climateZone;
                if (activities.Count > 0) options["activities"] = activities;

                var request = new JObject
                {
                    ["destination"] = destObj,
                    ["trip"] = new JObject
                    {
                        ["start_date"] = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["end_date"] = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    },
                    ["planner"] = planner,
                    ["options"] = options
                };

                var result = await _service.PackAsync(request);

                if (result == null)
                    return Reply(false, "The Packing Assistant API returned no result.");

                if (!result.Success)
                    return Reply(false, result.Error ?? "The Packing Assistant API reported an error.");

                if (result.Data == null)
                    return Reply(false, "The API did not return packing data.");

                if (result.Data.Type != JTokenType.Object)
                    return Reply(false, "The API returned data in an unexpected format. Root type: " + result.Data.Type + ".");

                JObject data = (JObject)result.Data;

                int mlCount = CountOf(data, "ml_items");
                int essCount = CountOf(data, "essentials");
                int extraCount = CountOf(data, "extra_weather_suggestions");

                Log("Service data root type: " + data.Type +
                    " | ml_items=" + mlCount +
                    " | essentials=" + essCount +
                    " | extra_weather_suggestions=" + extraCount);

                if (mlCount + essCount + extraCount == 0)
                    return Reply(false, "The API returned no packing items for this trip. Please check the city and country, or try different dates.");

                return Reply(true, null, data);
            }
            catch (Exception ex)
            {
                Log("Generate failed: " + ex.GetType().Name + ": " + ex.Message);
                return Reply(false, "An unexpected error occurred while generating your packing list. Please try again.");
            }
        }

        // Builds the response with Newtonsoft, not with Json().
        // Json() uses JavaScriptSerializer, which turns a JToken into nested arrays.
        private ContentResult Reply(bool success, string error, JToken data = null)
        {
            var envelope = new JObject
            {
                ["success"] = success,
                ["error"] = error == null ? JValue.CreateNull() : new JValue(error),
                ["data"] = data == null ? JValue.CreateNull() : data.DeepClone()
            };

            string body = envelope.ToString(Formatting.None);

            if (VerboseLogging)
            {
                string dataType = envelope["data"].Type.ToString();
                Log("MVC response: success=" + success +
                    " | data type=" + dataType +
                    " | length=" + body.Length +
                    " | start=" + (body.Length > 500 ? body.Substring(0, 500) : body));
            }

            return Content(body, "application/json");
        }

        private static int CountOf(JObject data, string key)
        {
            JArray arr = data[key] as JArray;
            return arr == null ? 0 : arr.Count;
        }

        private static void Log(string message)
        {
            if (!VerboseLogging) return;
            Debug.WriteLine("[PackingAssistant] " + message);
        }

        private static string NullIfBlank(string value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static int Clamp(int value, int fallback)
            => (value < 1 || value > 5) ? fallback : value;
    }

    public class PackingFormModel
    {
        public string Destination { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string FromRecommender { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Seniors { get; set; }
        public string TripType { get; set; }
        public string AgeGroup { get; set; }
        public string PackingPreference { get; set; }
        public bool? LaundryAvailable { get; set; }
        public string Transportation { get; set; }
        public string Accommodation { get; set; }
        public string Activities { get; set; }
        public string ClimateZone { get; set; }
        public string SpecialNeeds { get; set; }
        public double? Temperature { get; set; }
        public string Budget { get; set; }
        public string Region { get; set; }
        public int Culture { get; set; }
        public int Adventure { get; set; }
        public int Nature { get; set; }
        public int Beaches { get; set; }
        public int Nightlife { get; set; }
        public int Cuisine { get; set; }
        public int Wellness { get; set; }
        public int Urban { get; set; }
        public int Seclusion { get; set; }
    }
}