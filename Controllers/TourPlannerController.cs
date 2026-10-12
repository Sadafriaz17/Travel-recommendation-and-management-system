using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class TourPlannerController : Controller
    {
        private readonly TourPlannerService _service = new TourPlannerService();

        // GET: /TourPlanner
        public ActionResult Index()
        {
            ViewBag.IsReady = AiSettings.IsTourReady;
            return View();
        }

        // POST: /TourPlanner/Generate  (called via AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Generate(TourPlannerFormModel form)
        {
            // --- Server-side validation ---
            if (string.IsNullOrWhiteSpace(form.FromCity))
                return Json(new { success = false, error = "Starting city is required." });

            if (string.IsNullOrWhiteSpace(form.Destination))
                return Json(new { success = false, error = "Destination is required." });

            // Resolve dates: if explicit dates are given, use them.
            // Otherwise, synthesise them from Duration (default = 5 days).
            string resolvedStart = form.StartDate;
            string resolvedEnd   = form.EndDate;

            bool hasDates = !string.IsNullOrWhiteSpace(form.StartDate)
                         && !string.IsNullOrWhiteSpace(form.EndDate);

            if (!hasDates)
            {
                int dur = (form.Duration > 0 && form.Duration <= 30) ? form.Duration : 5;
                var today = DateTime.Today;
                resolvedStart = today.ToString("yyyy-MM-dd");
                resolvedEnd   = today.AddDays(dur).ToString("yyyy-MM-dd");
            }

            if (!DateTime.TryParse(resolvedStart, out var sd) || !DateTime.TryParse(resolvedEnd, out var ed))
                return Json(new { success = false, error = "Invalid date format." });

            if (ed <= sd)
                return Json(new { success = false, error = "End date must be after start date." });

            if ((ed - sd).Days > 30)
                return Json(new { success = false, error = "Trip length cannot exceed 30 days." });

            if (form.Travelers < 1 || form.Travelers > 50)
                form.Travelers = 1;

            // Parse interests (comma-separated string from browser)
            var interests = new List<string>();
            if (!string.IsNullOrWhiteSpace(form.Interests))
                foreach (var i in form.Interests.Split(','))
                {
                    var t = i.Trim().ToLower();
                    if (!string.IsNullOrEmpty(t)) interests.Add(t);
                }

            // Add TripStyle into interests if provided
            if (!string.IsNullOrWhiteSpace(form.TripStyle))
            {
                var ts = form.TripStyle.Trim().ToLower();
                if (!interests.Contains(ts)) interests.Add(ts);
            }

            // Include climate/temperature preference in notes if provided
            string healthNotes = form.HealthNotes;
            if (!string.IsNullOrWhiteSpace(form.Temperature))
            {
                var climate = "Preferred climate: " + form.Temperature.Trim();
                healthNotes = string.IsNullOrWhiteSpace(healthNotes) ? climate : healthNotes + ". " + climate;
            }

            var req = new TourPlannerRequest
            {
                FromCity     = form.FromCity,
                Destination  = form.Destination,
                StartDate    = resolvedStart,
                EndDate      = resolvedEnd,
                BudgetMin    = form.BudgetMin,
                BudgetMax    = form.BudgetMax,
                Currency     = "USD",
                Travelers    = form.Travelers,
                Interests    = interests,
                TravelStyle  = form.TravelStyle,
                HealthNotes  = healthNotes
            };

            var result = await _service.GenerateAsync(req);

            if (!result.Success)
                return Json(new { success = false, error = result.Error });

            return Json(new { success = true, data = result.Data });
        }
    }

    // Form model — only used in this controller
    public class TourPlannerFormModel
    {
        public string  FromCity     { get; set; }
        public string  Destination  { get; set; }
        public string  StartDate    { get; set; }
        public string  EndDate      { get; set; }
        public int     Duration     { get; set; }   // days, used when no explicit dates
        public decimal BudgetMin    { get; set; }
        public decimal BudgetMax    { get; set; }
        public int     Travelers    { get; set; }
        public string  Interests    { get; set; }   // comma-separated
        public string  TravelStyle  { get; set; }   // itinerary pace: relaxed|balanced|packed
        public string  TripStyle    { get; set; }   // travel style: Luxury|Budget|Adventure|Family|Romantic
        public string  Temperature  { get; set; }   // free-text climate hint
        public string  HealthNotes  { get; set; }
    }
}
