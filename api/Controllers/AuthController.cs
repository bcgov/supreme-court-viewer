using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Models.auth;
using Scv.Db.Models;
using Scv.Db.Models.Auth;

namespace Scv.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public ScvDbContext Db { get; }
        public IConfiguration Configuration { get; }
        private AesGcmEncryption AesGcmEncryption { get; }

        public AuthController(ScvDbContext db, IConfiguration configuration, AesGcmEncryption aesGcmEncryption)
        {
            Db = db;
            Configuration = configuration;
            AesGcmEncryption = aesGcmEncryption;
        }
        /// <summary>
        /// This cannot be called from AJAX or SWAGGER. It must be loaded in the browser location, because it brings the user to the SSO page.
        /// </summary>
        /// <param name="redirectUri">URL to go back to.</param>
        [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
        [HttpGet("login")]
        public IActionResult Login(string redirectUri = "/api")
        {
            return Redirect(redirectUri);
        }

        /// <summary>
        /// Logout function, should wipe out all cookies.
        /// </summary>
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            var logoutUrl = $"{Configuration.GetNonEmptyValue("Keycloak:Authority")}/protocol/openid-connect/logout";

            var forwardedHost = HttpContext.Request.Headers.ContainsKey("X-Forwarded-Host")
                ? HttpContext.Request.Headers["X-Forwarded-Host"].ToString()
                : Request.Host.ToString();
            var forwardedPort = HttpContext.Request.Headers["X-Forwarded-Port"];

            //We are always sending X-Forwarded-Port, only time we aren't is when we are hitting the API directly.
            var baseUri = HttpContext.Request.Headers.ContainsKey("X-Forwarded-Host") ? $"{HttpContext.Request.Headers["X-Base-Href"]}logout" : "/api";

            var applicationUrl = $"{XForwardedForHelper.BuildUrlString(forwardedHost, forwardedPort, baseUri)}";
            var keycloakLogoutUrl = $"{logoutUrl}?post_logout_redirect_uri={applicationUrl}";
            var siteMinderLogoutUrl = $"{Configuration.GetNonEmptyValue("SiteMinderLogoutUrl")}?returl={keycloakLogoutUrl}&retnow=1";
            return Redirect(siteMinderLogoutUrl);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("request-civil-file-access")]
        public async Task<IActionResult> RequestCivilFileAccess([FromBody] RequestCivilFileAccess request)
        {
            return await RequestFileAccess(request, Request);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("request-criminal-file-access")]
        public async Task<IActionResult> RequestCriminalFileAccess([FromBody] RequestCriminalFileAccess request)
        {
            return await RequestFileAccess(request, Request);
        }

        private async Task<IActionResult> RequestFileAccess(IFileAccessRequest accessRequest, Microsoft.AspNetCore.Http.HttpRequest httpRequest)
        {
            // Default Civil Request
            string expiryVariable = "RequestCivilFileAccessMinutes";
            string urlPath = "civil-file";
            if (accessRequest is RequestCriminalFileAccess)
            {
                expiryVariable = "RequestCriminalFileAccessMinutes";
                urlPath = "criminal-file";
            }

            if (string.IsNullOrEmpty(accessRequest.FileId) || string.IsNullOrEmpty(accessRequest.UserId))
                return BadRequest();

            if (!User.IsServiceAccountUser())
                return Forbid();

            var agencyId = string.IsNullOrEmpty(accessRequest.AgencyId) ? "" : AesGcmEncryption.Encrypt(accessRequest.AgencyId);
            var partId = string.IsNullOrEmpty(accessRequest.PartId) ? "" : AesGcmEncryption.Encrypt(accessRequest.PartId);

            var expiryMinutes = float.Parse(Configuration.GetValue($"{expiryVariable}", "15.0"));
            await Db.RequestFileAccess.AddAsync(new RequestFileAccess
            {
                FileId = accessRequest.FileId,
                UserId = accessRequest.UserId,
                UserName = accessRequest.UserName,
                AgencyId = agencyId,
                PartId = partId,
                Requested = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            });
            await Db.SaveChangesAsync();

            var forwardedHost = httpRequest.Headers["X-Forwarded-Host"];
            var forwardedPort = httpRequest.Headers["X-Forwarded-Port"];
            var baseUrl = httpRequest.Headers["X-Base-Href"];

            return Ok(new
            {
                Url = XForwardedForHelper.BuildUrlString(
                    forwardedHost,
                    forwardedPort,
                    baseUrl,
                    $"{urlPath}/{accessRequest.FileId}",
                    "fromA2A=true")
            });
        }

        /// <summary>
        /// Provides a way for the front-end to get info about the user.
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
        [HttpGet]
        [Route("info")]
        public ActionResult UserInfo()
        {
            string userType;
            if (HttpContext.User.IsIdirUser())
                userType = "idir";
            else if (HttpContext.User.IsVcUser())
                userType = "vc";
            else
                userType = "judiciary";

            return Ok(new
            {
                UserType = userType,
                EnableArchive = false,
                Role = HttpContext.User.Role(),
                SubRole = HttpContext.User.SubRole(),
                IsSupremeUser = HttpContext.User.IsSupremeUser(),
                DateTime.UtcNow
            });
        }
    }
}
