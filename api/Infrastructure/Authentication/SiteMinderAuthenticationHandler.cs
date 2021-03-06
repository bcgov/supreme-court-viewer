﻿using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.JCUserService;
using Scv.Api.Services;

namespace Scv.Api.Infrastructure.Authentication
{
    public class SiteMinderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        #region Properties 
        public const string SiteMinder = nameof(SiteMinder);
        private JCUserService JCUserService { get; }
        private string ValidSiteMinderUserType { get; }
        #endregion

        #region Constructor
        public SiteMinderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration, JCUserService jcUserService) : base(options, logger, encoder, clock)
        {
            JCUserService = jcUserService;
            ValidSiteMinderUserType = configuration.GetNonEmptyValue("Auth:AllowSiteMinderUserType");
        }
        #endregion

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string siteMinderUserGuidHeader = Request.Headers["SMGOV_USERGUID"];
            string siteMinderUserTypeHeader = Request.Headers["SMGOV_USERTYPE"];

            if (siteMinderUserGuidHeader == null || siteMinderUserTypeHeader == null)
                return AuthenticateResult.NoResult();
            
            if (siteMinderUserTypeHeader != ValidSiteMinderUserType)
                return AuthenticateResult.Fail("Invalid SiteMinder credentials.");

            var authenticatedBySiteMinderPreviously = Context.User.Identity.AuthenticationType == SiteMinder;
            var participantId = Context.User.ParticipantId(); 
            var agencyCode = Context.User.AgencyCode();
            if (!authenticatedBySiteMinderPreviously)
            {
                var request = new UserInfoRequest
                {
                    DeviceName = Environment.MachineName,
                    DomainUserGuid = siteMinderUserGuidHeader,
                    DomainUserId = Request.Headers["SM_USER"],
                    IpAddress = Request.Headers["X-Real-IP"],
                    TemporaryAccessGuid = ""
                };
                var jcUserInfo = await JCUserService.GetUserInfo(request);
                if (jcUserInfo == null)
                    return AuthenticateResult.Fail("Couldn't authenticate through JC-Interface.");

                participantId = jcUserInfo.UserPartId;
                agencyCode = jcUserInfo.UserDefaultAgencyCd ?? "";
            }

            var claims = new[] {
                new Claim(CustomClaimTypes.JcParticipantId, participantId),
                new Claim(CustomClaimTypes.JcAgencyCode, agencyCode),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            if (!authenticatedBySiteMinderPreviously)
                await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
