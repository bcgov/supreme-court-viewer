using System;
using System.Threading.Tasks;
using JCCommon.Clients.UserService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Models.JCUserService;

namespace Scv.Api.Services
{
    public class JCUserService
    {
        #region Variables
        private UserServiceClient UserServiceClient { get; }
        private ILogger<JCUserService> Logger { get; }
        private string SupremeAgencyId { get; }
        #endregion Variables

        #region Constructor
        public JCUserService(UserServiceClient userServiceClient, ILogger<JCUserService> logger, IConfiguration configuration)
        {
            UserServiceClient = userServiceClient;
            UserServiceClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            Logger = logger;
            SupremeAgencyId = configuration.GetNonEmptyValue("Request:GetUserLoginDefaultAgencyId");
        }
        #endregion

        public async Task<GetUserLoginResponseType> GetUserInfo(UserInfoRequest userInfoRequest)
        {
            try
            {
                var response = await UserServiceClient.UserGetUserLoginAsync(userInfoRequest.DomainName,
                    userInfoRequest.DomainUserGuid,
                    userInfoRequest.DomainUserId,
                    userInfoRequest.DeviceName,
                    userInfoRequest.IpAddress,
                    userInfoRequest.TemporaryAccessGuid);

                if (response.ResponseCd != "1")
                {
                    response.UserDefaultAgencyCd = string.IsNullOrEmpty(response.UserDefaultAgencyCd) ? SupremeAgencyId : response.UserDefaultAgencyCd;
                    Logger.LogDebug($"SMGOV_USERGUID: {userInfoRequest.DomainUserGuid}, UserAgencyCd: {response.UserDefaultAgencyCd}, UserPartId: {response.UserPartId}");
                    return response;
                }
                Logger.LogInformation("Returned responseCd = 1 (failed) from getUserLogin");
                return null;
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return null;
            }
        }
    }
}
