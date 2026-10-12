using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using TourwebsiteFYP.Services;

namespace TourwebsiteFYP.Controllers
{
    public class PackingAssistantController : Controller
    {
        private readonly PackingAssistantService _service = new PackingAssistantService();

        // GET: /PackingAssistant
        public ActionResult Index()
        {
            ViewBag.IsReady = AiSettings.IsPackingReady;
            return View();
        }

        // POST: /PackingAssistant/Generate  (called via AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Generate(PackingFormModel form)
        {
            // --- Server-side validation ---
            if (string.IsNullOrWhiteSpace(form.Destination))
                return Json(new { success = false, error = "Destination is required." });

            if (form.Days < 1 || form.Days > 365)
                return Json(new { success = false, error = "Days must be between 1 and 365." });

            if (string.IsNullOrWhiteSpace(form.TripType))
                return Json(new { success = false, error = "Trip type is required." });

            // Build service request
            var req = new PackingRequest
            {
                Destination  = form.Destination,
                Days         = form.Days,
                StartDate    = form.StartDate,
                EndDate      = form.EndDate,
                TripType     = form.TripType,
                Weather      = form.Weather,
                SpecialNeeds = form.SpecialNeeds,
                Travelers    = new List<TravelerGroup>()
            };

            if (form.Adults   > 0) req.Travelers.Add(new TravelerGroup { AgeGroup = "adult",  Count = form.Adults   });
            if (form.Children > 0) req.Travelers.Add(new TravelerGroup { AgeGroup = "child",  Count = form.Children });
            if (form.Seniors  > 0) req.Travelers.Add(new TravelerGroup { AgeGroup = "senior", Count = form.Seniors  });

            if (req.Travelers.Count == 0)
                req.Travelers.Add(new TravelerGroup { AgeGroup = "adult", Count = 1 });

            var result = await _service.GenerateAsync(req);

            if (!result.Success)
                return Json(new { success = false, error = result.Error });

            // Return JSON to the browser; the key was never in this JSON
            return Json(new { success = true, data = result.Data });
        }
    }

    // Form model — only used in this controller
    public class PackingFormModel
    {
        public string Destination  { get; set; }
        public int    Days         { get; set; }
        public string StartDate    { get; set; }
        public string EndDate      { get; set; }
        public string TripType     { get; set; }
        public int    Adults       { get; set; }
        public int    Children     { get; set; }
        public int    Seniors      { get; set; }
        public string Weather      { get; set; }
        public string SpecialNeeds { get; set; }
    }
}
