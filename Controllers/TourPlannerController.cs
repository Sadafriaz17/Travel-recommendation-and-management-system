using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class TourPlannerController : Controller
    {
        private const string DateFormat = "yyyy-MM-dd";

        private static readonly string[] Budgets =
        {
            "Budget", "Mid-range", "Luxury"
        };

        private static readonly string[] Regions =
        {
            "africa", "asia", "europe", "middle_east",
            "north_america", "oceania", "south_america"
        };

        private readonly NexVoyService _service = new NexVoyService();

        private readonly PackingAssistantService _packing =
            new PackingAssistantService();

        public ActionResult Index()
        {
            ViewBag.IsReady = AiSettings.IsNexVoyReady;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Recommend(NexVoyFormModel form)
        {
            if (form == null)
            {
                return Reply(false, "Invalid recommendation request.");
            }

            if (!AiSettings.IsNexVoyReady)
            {
                return Reply(false, "Tour Recommendation service is not configured.");
            }

            if (!form.Temperature.HasValue)
            {
                return Reply(false, "Preferred temperature is required.");
            }

            if (form.Temperature.Value < -50 || form.Temperature.Value > 50)
            {
                return Reply(false, "Temperature must be between -50 and 50 °C.");
            }

            string budget = (form.Budget ?? "").Trim();

            if (!Budgets.Contains(budget))
            {
                return Reply(false, "Please choose a budget: Budget, Mid-range or Luxury.");
            }

            string region = string.IsNullOrWhiteSpace(form.Region)
                ? null
                : form.Region.Trim().ToLowerInvariant();

            if (region != null && !Regions.Contains(region))
            {
                return Reply(false, "Unknown region.");
            }

            int[] ratings =
            {
                form.Culture, form.Adventure, form.Nature,
                form.Beaches, form.Nightlife, form.Cuisine,
                form.Wellness, form.Urban, form.Seclusion
            };

            if (ratings.Any(r => r < 1 || r > 5))
            {
                return Reply(false, "Every preference rating must be between 1 and 5.");
            }

            string startRaw = (form.TravelStartDate ?? "").Trim();
            string endRaw = (form.TravelEndDate ?? "").Trim();

            bool hasStart = startRaw.Length > 0;
            bool hasEnd = endRaw.Length > 0;

            if (hasStart != hasEnd)
            {
                return Reply(false, "Please provide both travel dates, or leave both empty.");
            }

            DateTime startDate = default(DateTime);
            DateTime endDate = default(DateTime);

            if (hasStart)
            {
                if (!DateTime.TryParseExact(startRaw, DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate) ||
                    !DateTime.TryParseExact(endRaw, DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                {
                    return Reply(false, "Travel dates must be valid dates.");
                }

                if (startDate > endDate)
                {
                    return Reply(false, "Travel start date cannot be after the end date.");
                }
            }

            var req = new NexVoyRequest
            {
                Temperature = form.Temperature.Value,
                Budget = budget,
                Region = region,
                TopN = 5,

                Preferences = new NexVoyPreferences
                {
                    Culture = form.Culture,
                    Adventure = form.Adventure,
                    Nature = form.Nature,
                    Beaches = form.Beaches,
                    Nightlife = form.Nightlife,
                    Cuisine = form.Cuisine,
                    Wellness = form.Wellness,
                    Urban = form.Urban,
                    Seclusion = form.Seclusion
                }
            };

            if (hasStart)
            {
                req.TravelStartDate = startDate.ToString(DateFormat, CultureInfo.InvariantCulture);
                req.TravelEndDate = endDate.ToString(DateFormat, CultureInfo.InvariantCulture);
                req.TravelMonth = startDate.Month;
            }

            var result = await _service.RecommendAsync(req);

            if (result == null || !result.Success)
            {
                return Reply(false, result != null ? result.Error
                    : "Could not get destination recommendations.");
            }

            if (result.Data == null ||
                result.Data.Results == null ||
                result.Data.Results.Count == 0)
            {
                return Reply(false, "No destinations were returned. Please try different preferences.");
            }

            return Reply(true, null, result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Packing(NexVoyPackForm f)
        {
            if (f == null) return Reply(false, "Missing packing request.");

            if (!AiSettings.IsPackAssistantReady)
            {
                return Reply(false, "The new Packing Assistant is not configured.");
            }

            string city = Clean(f.City, 100);
            string country = Clean(f.Country, 100);

            if (city.Length == 0 || country.Length == 0)
            {
                return Reply(false, "Destination city and country are required.");
            }

            string startRaw = (f.StartDate ?? "").Trim();
            string endRaw = (f.EndDate ?? "").Trim();

            if (startRaw.Length == 0 || endRaw.Length == 0)
            {
                return Reply(false, "A packing list requires both travel dates. Please select your trip dates in the Tour Planner.");
            }

            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(startRaw, DateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate) ||
                !DateTime.TryParseExact(endRaw, DateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
            {
                return Reply(false, "Travel dates must be valid dates.");
            }

            if (startDate > endDate)
            {
                return Reply(false, "Travel start date cannot be after the end date.");
            }

            if (startDate.Date < DateTime.Today)
            {
                return Reply(false, "Travel start date cannot be in the past.");
            }

            if ((endDate - startDate).Days + 1 > 60)
            {
                return Reply(false, "Trips longer than 60 days are not supported.");
            }

            var destination = new JObject { ["city"] = city, ["country"] = country };

            string destinationRegion = Clean(f.DestRegion, 50)
                .ToLowerInvariant().Replace(' ', '_');

            if (Regions.Contains(destinationRegion))
            {
                destination["region"] = destinationRegion;
            }

            double? typicalTemperature = Num(f.TypicalAvgTemp);
            double? forecastTemperature = Num(f.ForecastAvgTemp);

            if (typicalTemperature.HasValue)
            {
                destination["typical_avg_temp_c"] = typicalTemperature.Value;
            }

            if (forecastTemperature.HasValue)
            {
                destination["forecast_avg_temp_c"] = forecastTemperature.Value;
            }

            string idealTripDays = Clean(f.IdealTripDays, 50);
            if (idealTripDays.Length > 0) destination["ideal_trip_days"] = idealTripDays;

            int[] ratings =
            {
                f.Culture, f.Adventure, f.Nature, f.Beaches,
                f.Nightlife, f.Cuisine, f.Wellness, f.Urban, f.Seclusion
            };

            if (ratings.Any(r => r < 1 || r > 5))
            {
                return Reply(false, "Every preference rating must be between 1 and 5.");
            }

            var planner = new JObject();

            double? preferredTemperature = Num(f.Temperature);

            if (!preferredTemperature.HasValue)
            {
                return Reply(false, "Original preferred temperature is missing. Please run the Tour Planner again.");
            }

            if (preferredTemperature.Value < -50 || preferredTemperature.Value > 50)
            {
                return Reply(false, "Temperature must be between -50 and 50 °C.");
            }

            planner["preferred_temperature_c"] = preferredTemperature.Value;

            string budget = (f.Budget ?? "").Trim();

            if (!Budgets.Contains(budget))
            {
                return Reply(false, "Original planner budget is missing or invalid. Please run the Tour Planner again.");
            }

            planner["budget"] = budget;

            string region = string.IsNullOrWhiteSpace(f.Region)
                ? null : f.Region.Trim().ToLowerInvariant();

            if (region != null)
            {
                if (!Regions.Contains(region)) return Reply(false, "Unknown planner region.");
                planner["region"] = region;
            }

            planner["preferences"] = new JObject
            {
                ["culture"] = f.Culture,
                ["adventure"] = f.Adventure,
                ["nature"] = f.Nature,
                ["beaches"] = f.Beaches,
                ["nightlife"] = f.Nightlife,
                ["cuisine"] = f.Cuisine,
                ["wellness"] = f.Wellness,
                ["urban"] = f.Urban,
                ["seclusion"] = f.Seclusion
            };

            var request = new JObject
            {
                ["destination"] = destination,

                ["trip"] = new JObject
                {
                    ["start_date"] = startDate.ToString(DateFormat, CultureInfo.InvariantCulture),
                    ["end_date"] = endDate.ToString(DateFormat, CultureInfo.InvariantCulture)
                },

                ["planner"] = planner,
                ["options"] = new JObject()
            };

            var result = await _packing.PackAsync(request);

            if (result == null || !result.Success)
            {
                return Reply(false, result != null ? result.Error : "Could not generate the packing list.");
            }

            if (result.Data == null)
            {
                return Reply(false, "Packing Assistant returned an empty response.");
            }

            return Reply(true, null, result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenPackingAssistant(NexVoyOpenPackForm f)
        {
            if (f == null)
            {
                TempData["PackingError"] = "Missing destination details. Please try again.";
                return RedirectToAction("Index");
            }

            string city = Clean(f.City, 100);
            string country = Clean(f.Country, 100);

            if (city.Length == 0 || country.Length == 0)
            {
                TempData["PackingError"] = "Destination city and country are required.";
                return RedirectToAction("Index");
            }

            string startRaw = (f.StartDate ?? "").Trim();
            string endRaw = (f.EndDate ?? "").Trim();
            int days = 0;

            if (startRaw.Length > 0 && endRaw.Length > 0)
            {
                DateTime startDate;
                DateTime endDate;

                if (DateTime.TryParseExact(startRaw, DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate) &&
                    DateTime.TryParseExact(endRaw, DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate) &&
                    endDate >= startDate)
                {
                    days = (endDate - startDate).Days + 1;
                }
                else
                {
                    startRaw = "";
                    endRaw = "";
                }
            }

            var prefs = new Dictionary<string, int>
            {
                ["Culture"] = f.Culture,
                ["Adventure"] = f.Adventure,
                ["Nature"] = f.Nature,
                ["Beaches"] = f.Beaches,
                ["Nightlife"] = f.Nightlife,
                ["Cuisine"] = f.Cuisine,
                ["Wellness"] = f.Wellness,
                ["Urban"] = f.Urban,
                ["Seclusion"] = f.Seclusion
            };

            TempData["PackingPrefill"] = new PackingPrefill
            {
                City = city,
                Country = country,
                StartDate = startRaw,
                EndDate = endRaw,
                Days = days,
                Temperature = f.Temperature,
                Budget = f.Budget,
                Region = f.Region,
                Prefs = prefs
            };

            return RedirectToAction("Index", "PackingAssistant");
        }

        private static string Clean(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            value = new string(value.Trim().Where(c => !char.IsControl(c)).ToArray());

            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        private static double? Num(string value)
        {
            double number;

            if (!string.IsNullOrWhiteSpace(value) &&
                double.TryParse(value.Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out number) &&
                !double.IsNaN(number) && !double.IsInfinity(number))
            {
                return number;
            }

            return null;
        }

        private ContentResult Reply(bool success, string error, object data = null)
        {
            return Content(
                JsonConvert.SerializeObject(new
                {
                    success = success,
                    error = error,
                    data = data
                }),
                "application/json");
        }
    }

    // =========================================================
    // TOUR RECOMMENDATION FORM MODELS
    // =========================================================

    public class NexVoyFormModel
    {
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
        public string TravelStartDate { get; set; }
        public string TravelEndDate { get; set; }
    }

    public class NexVoyPackForm
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string DestRegion { get; set; }
        public string TypicalAvgTemp { get; set; }
        public string ForecastAvgTemp { get; set; }
        public string IdealTripDays { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Temperature { get; set; }
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

    public class NexVoyOpenPackForm
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
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

    public class PackingPrefill
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int Days { get; set; }
        public double? Temperature { get; set; }
        public string Budget { get; set; }
        public string Region { get; set; }
        public Dictionary<string, int> Prefs { get; set; }
    }
}