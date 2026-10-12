using System;
using System.Configuration;

namespace TourwebsiteFYP.Services
{
    /// <summary>
    /// Reads AI tool configuration from appSettings.
    /// Paste your values into Web.secrets.config (which is excluded from source control).
    /// </summary>
    public static class AiSettings
    {
        public static string ApiKey          => ConfigurationManager.AppSettings["AI:ApiKey"]          ?? "";
        public static string PackingApiUrl   => ConfigurationManager.AppSettings["AI:PackingApiUrl"]   ?? "";
        public static string TourPlannerApiUrl => ConfigurationManager.AppSettings["AI:TourPlannerApiUrl"] ?? "";
        public static int    TimeoutSeconds  => int.TryParse(ConfigurationManager.AppSettings["AI:TimeoutSeconds"], out var t) ? t : 60;

        /// <summary>Returns true only when both the key and the requested URL are non-empty.</summary>
        public static bool IsPackingReady   => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(PackingApiUrl);
        public static bool IsTourReady      => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(TourPlannerApiUrl);
    }
}
