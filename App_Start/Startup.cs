// ============================================================
// OWIN STARTUP - Google OAuth configuration
// Client ID and Client Secret come from Web.secrets.config
// (keys: GoogleClientId and GoogleClientSecret).
// Redirect URI added in Google Cloud Console:
//   https://localhost:44373/signin-google
// ============================================================

using System;
using System.Configuration;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartup(typeof(TourwebsiteFYP.Startup))]

namespace TourwebsiteFYP
{
    public class Startup
    {
        /// <summary>Claim added after Google sign-in: "true" only when Google says the email is verified.</summary>
        public const string VerifiedEmailClaim = "urn:google:email_verified";

        /// <summary>True when a real Google Client ID and Secret were found in the settings.</summary>
        public static bool GoogleConfigured { get; private set; }

        /// <summary>Last warning or error written by the OWIN Google login. Shown only while debug is on.</summary>
        public static string LastOwinProblem { get; set; }

        public void Configuration(IAppBuilder app)
        {
            // Catch the real reason when Google sign-in fails.
            app.SetLoggerFactory(new OwinProblemLoggerFactory());

            // External cookie holds the Google identity for a moment, between the
            // Google redirect and our ExternalLoginCallback action.
            app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ExternalCookie);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ExternalCookie
            });

            string clientId = (ConfigurationManager.AppSettings["GoogleClientId"] ?? "").Trim();
            string clientSecret = (ConfigurationManager.AppSettings["GoogleClientSecret"] ?? "").Trim();

            GoogleConfigured = LooksValid(clientId, clientSecret);
            if (!GoogleConfigured)
            {
                // Do not register Google with bad values. The controller shows a clear message.
                return;
            }

            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                CallbackPath = new PathString("/signin-google"),
                Provider = new GoogleOAuth2AuthenticationProvider
                {
                    OnAuthenticated = context =>
                    {
                        // Google sends "verified_email" with the profile.
                        JToken token = context.User["verified_email"];
                        bool verified = token != null
                                        && token.Type == JTokenType.Boolean
                                        && (bool)token;

                        context.Identity.AddClaim(new Claim(VerifiedEmailClaim, verified ? "true" : "false"));
                        return Task.FromResult(0);
                    }
                }
            });
        }

        private static bool LooksValid(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return false;
            if (clientId.StartsWith("PASTE", StringComparison.OrdinalIgnoreCase)) return false;
            if (clientSecret.StartsWith("PASTE", StringComparison.OrdinalIgnoreCase)) return false;
            return clientId.EndsWith(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Creates loggers that remember the last OWIN warning or error.</summary>
    public class OwinProblemLoggerFactory : ILoggerFactory
    {
        public ILogger Create(string name)
        {
            return new OwinProblemLogger(name);
        }
    }

    public class OwinProblemLogger : ILogger
    {
        private readonly string _name;

        public OwinProblemLogger(string name)
        {
            _name = name;
        }

        public bool WriteCore(TraceEventType eventType, int eventId, object state,
                              Exception exception, Func<object, Exception, string> formatter)
        {
            // Lower number means more serious. Keep only warnings and worse.
            if (eventType > TraceEventType.Warning) return false;

            string text = formatter != null ? formatter(state, exception) : Convert.ToString(state);

            Exception ex = exception;
            while (ex != null)
            {
                text += " | " + ex.GetType().Name + ": " + ex.Message;
                ex = ex.InnerException;
            }

            Startup.LastOwinProblem = text;
            Debug.WriteLine("[OWIN " + _name + "] " + text);
            return true;
        }
    }
}