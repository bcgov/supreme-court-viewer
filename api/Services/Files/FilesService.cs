﻿using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;

namespace Scv.Api.Services.Files
{
    /// <summary>
    /// This is meant to wrap our FileServicesClient. That way we can easily extend the information provided to us by the FileServicesClient.
    /// </summary>
    public class FilesService
    {
        #region Variables

        public readonly CivilFilesService Civil;
        public readonly CriminalFilesService Criminal; 
        private readonly FileServicesClient _filesClient;
        private readonly IAppCache _cache;
        private readonly string _applicationCode;
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;

        #endregion Variables

        #region Constructor

        public FilesService(IConfiguration configuration,
            FileServicesClient filesClient,
            IMapper mapper,
            LookupService lookupService,
            LocationService locationService,
            IAppCache cache,
            ClaimsPrincipal claimsPrincipal,
            ILoggerFactory factory
            )
        {
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:FileExpiryMinutes")) * 60;
            Civil = new CivilFilesService(configuration, filesClient, mapper, lookupService, locationService, _cache, claimsPrincipal, factory.CreateLogger<CivilFilesService>());
            Criminal = new CriminalFilesService(configuration, filesClient, mapper, lookupService, locationService, _cache, claimsPrincipal);

            _applicationCode = claimsPrincipal.ApplicationCode();
            _requestAgencyIdentifierId = claimsPrincipal.AgencyCode();
            _requestPartId = claimsPrincipal.ParticipantId();
        }

        #endregion Constructor

        #region Methods
        
        #region Courtlist & Document     

        public async Task<FileResponse> DocumentAsync(string documentId, bool isCriminal, string physicalFileId)
        {
            var loggingId = Guid.NewGuid().ToString();
            return await _filesClient.FilesDocumentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, loggingId, documentId, isCriminal ? "R" : "I", physicalFileId, flatten: false);
        }

        #endregion Courtlist & Document

        #endregion Methods
    }
}