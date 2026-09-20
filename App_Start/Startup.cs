// ============================================================
// OWIN STARTUP — Google OAuth configuration
// ============================================================
// SETUP INSTRUCTIONS (Google Cloud Console):
//   1. Go to https://console.cloud.google.com → APIs & Services → Credentials
//   2. Click "Create Credentials" → "OAuth 2.0 Client ID"
//   3. Application type: Web application
//   4. Add Authorized redirect URI:
//        https://localhost:{port}/signin-google
//      (match the IIS Express port from TourwebsiteFYP.csproj.user / launchSettings)
//   5. Copy the Client ID and Client Secret into Web.config:
//        <add key="GoogleClientId"     value="PASTE_CLIENT_ID_HERE" />
//        <add key="GoogleClientSecret" value="PASTE_CLIENT_SECRET_HERE" />
// ============================================================

[assembly: Microsoft.Owin.OwinStartup(typeof(TourwebsiteFYP.Startup))]

namespace TourwebsiteFYP
{
    using System.Configuration;
    using Microsoft.AspNet.Identity;
    using Microsoft.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Owin.Security.Google;
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // External cookie holds the Google identity transiently between the
            // OAuth redirect and our ExternalLoginCallback action.
            app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ExternalCookie);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ExternalCookie
            });

            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
            {
                ClientId     = ConfigurationManager.AppSettings["GoogleClientId"],
                ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"]
            });
        }
    }
}
