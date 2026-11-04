using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class TourPlannerController : Controller
    {
        private static readonly string[] Budgets = { "Budget", "Mid-range", "Luxury" };
        private static readonly string[] Regions =
            { "africa", "asia", "europe", "middle_east", "north_america", "oceania", "south_america" };

        private readonly NexVoyService _service = new NexVoyService();

        // GET: /TourPlanner
        public ActionResult Index()
        {
            ViewBag.IsReady = AiSettings.IsNexVoyReady;
            return View();
        }

        // POST: /TourPlanner/Recommend  (called via AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Recommend(NexVoyFormModel form)
        {
            // --- Server-side validation ---
            if (form == null || form.Temperature == null)
                return Reply(false, "Preferred temperature is required.");

            if (form.Temperature < -50 || form.Temperature > 50)
                return Reply(false, "Temperature must be between -50 and 50 °C.");

            string budget = (form.Budget ?? "").Trim();
            if (!Budgets.Contains(budget))
                return Reply(false, "Please choose a budget: Budget, Mid-range or Luxury.");

            string region = string.IsNullOrWhiteSpace(form.Region) ? null : form.Region.Trim().ToLower();
            if (region != null && !Regions.Contains(region))
                return Reply(false, "Unknown region.");

            int[] ratings = { form.Culture, form.Adventure, form.Nature, form.Beaches, form.Nightlife,
                              form.Cuisine, form.Wellness, form.Urban, form.Seclusion };
            if (ratings.Any(r => r < 1 || r > 5))
                return Reply(false, "Every rating must be between 1 and 5.");

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

            var result = await _service.RecommendAsync(req);

            if (!result.Success)
                return Reply(false, result.Error);

            return Reply(true, null, result.Data);
        }

        // Newtonsoft keeps the API's field names (match_score, avg_temp, ...) that the page script reads.
        // The API key is never part of this response.
        private ContentResult Reply(bool success, string error, NexVoyResponse data = null)
        {
            return Content(JsonConvert.SerializeObject(new { success = success, error = error, data = data }),
                           "application/json");
        }
    }

    // Form model - only used in this controller
    public class NexVoyFormModel
    {
        public double? Temperature { get; set; }   // preferred average temperature, °C
        public string Budget { get; set; }   // Budget | Mid-range | Luxury
        public string Region { get; set; }   // optional, e.g. "europe"
        public int Culture { get; set; }          // ratings 1-5
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
