using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.LookupServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Exceptions;
using CodeLookup = System.Collections.Generic.ICollection<JCCommon.Clients.LookupServices.LookupCode>;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and LookupServicesClient.
    /// </summary>
    public class LookupService
    {
        #region Variables
        private readonly IAppCache _cache;
        private readonly IConfiguration _configuration;
        private readonly LookupServiceClient _lookupClient;

        public const string LookupDocumentDescriptions = nameof(LookupDocumentDescriptions);
        public const string LookupRoleTypeDescriptions = nameof(LookupRoleTypeDescriptions);
        #endregion

        #region Properties
        private DateTimeOffset CacheExpiry => DateTimeOffset.Now.AddHours(1);
        #endregion

        #region Constructor
        public LookupService(IConfiguration configuration, LookupServiceClient lookupClient, IAppCache cache)
        {
            _configuration = configuration;
            _lookupClient = lookupClient;
            _cache = cache;
            SetupLookupServicesClient();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gives a document description, based on the provided document code.
        /// </summary>
        /// <param name="documentCode"></param>
        /// <returns></returns>
        public async Task<string> GetDocumentDescriptionAsync(string documentCode)
        {
            var documentLookupCodes = await GetDocumentCodesFromLazyCache();
            return documentLookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == documentCode)?.ShortDesc ?? "";
        }

        /// <summary>
        ///  Uses LazyCache to query LookupServiceClient for lookup codes. This is about 600 codes atm.
        /// </summary>
        /// <returns></returns>
        public async Task<CodeLookup> GetDocumentCodesFromLazyCache() => await _cache.GetOrAddAsync(LookupDocumentDescriptions, async () => await _lookupClient.CodesDocumentsAsync(), CacheExpiry);

        /// <summary>
        /// Reads from the configuration for the document category.
        /// </summary>
        /// <param name="documentCode"></param>
        /// <returns>string</returns>
        public string GetDocumentCategory(string documentCode)
        {
            var configurationSections =
                _configuration.GetSection("DocumentCategories").Get<Dictionary<string, string>>() ??
                throw new ConfigurationException("Couldn't not build dictionary based on DocumentCategories");
            return configurationSections.FirstOrDefault(cs => cs.Value.Split(",").Contains(documentCode)).Key ?? "";
        }

        public async Task<string> GetCivilRoleTypeDescription(string roleTypeCode)
        {
            var roleCodes = await GetRoleTypeCodesFromLazyCache();
            return roleCodes.FirstOrDefault(lookupCode => lookupCode.Code == roleTypeCode)?.ShortDesc ?? "";
        }

        public async Task<CodeLookup> GetRoleTypeCodesFromLazyCache() => await _cache.GetOrAddAsync(LookupRoleTypeDescriptions, async () => await _lookupClient.CodesRolesAsync(), CacheExpiry);
        #endregion

        #region Helpers

        private void SetupLookupServicesClient()
        {
            _lookupClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver();
            _lookupClient.BaseUrl = _configuration.GetValue<string>("LookupServicesClient:Url") ?? throw new ConfigurationException($"Configuration 'LookupServicesClient:Url' is invalid or missing.");
        }
        #endregion

    }
}
