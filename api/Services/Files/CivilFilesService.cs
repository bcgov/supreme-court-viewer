﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using Scv.Api.Helpers.Exceptions;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Civil.AppearanceDetail;
using Scv.Api.Models.Civil.Appearances;
using Scv.Api.Models.Civil.CourtList;
using Scv.Api.Models.Civil.Detail;
using Scv.Api.Models.Search;
using CivilAppearanceDetail = Scv.Api.Models.Civil.AppearanceDetail.CivilAppearanceDetail;
using CivilAppearanceMethod = Scv.Api.Models.Civil.AppearanceDetail.CivilAppearanceMethod;

namespace Scv.Api.Services.Files
{
    public class CivilFilesService
    {
        #region Variables

        private readonly ILogger<CivilFilesService> _logger;
        private readonly IAppCache _cache;
        private readonly FileServicesClient _filesClient;
        private readonly IMapper _mapper;
        private readonly LookupService _lookupService;
        private readonly LocationService _locationService;
        private readonly string _applicationCode;
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;
        private List<string> _filterOutDocumentTypes;

        #endregion Variables

        #region Constructor

        public CivilFilesService(IConfiguration configuration,
            FileServicesClient filesClient,
            IMapper mapper,
            LookupService lookupService,
            LocationService locationService,
            IAppCache cache,
            ClaimsPrincipal user,
            ILogger<CivilFilesService> logger)
        {
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _lookupService = lookupService;
            _locationService = locationService;
            _mapper = mapper;
            _cache = cache;
            _applicationCode = user.ApplicationCode();
            _requestAgencyIdentifierId = user.AgencyCode();
            _requestPartId = user.ParticipantId();
            _logger = logger;
            _filterOutDocumentTypes = configuration.GetNonEmptyValue("ExcludeDocumentTypeCodesForCounsel").Split(",").ToList();
        }

        #endregion Constructor

        #region Methods

        public async Task<FileSearchResponse> SearchAsync(FilesCivilQuery fcq)
        {
            fcq.FilePermissions =
                "[\"A\", \"Y\", \"T\", \"F\", \"C\", \"M\", \"L\", \"R\", \"B\", \"D\", \"E\", \"G\", \"H\", \"N\", \"O\", \"P\", \"S\", \"V\"]"; // for now, use all types - TODO: determine proper list of types?
            return await _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId,
                _applicationCode, fcq.SearchMode, fcq.FileHomeAgencyId, fcq.FileNumber, fcq.FilePrefix,
                fcq.FilePermissions, fcq.FileSuffixNumber, fcq.MDocReferenceTypeCode, fcq.CourtClass, fcq.CourtLevel,
                fcq.NameSearchType, fcq.LastName, fcq.OrgName, fcq.GivenName, fcq.Birth?.ToString("yyyy-MM-dd"),
                fcq.SearchByCrownPartId, fcq.SearchByCrownActiveOnly, fcq.SearchByCrownFileDesignation,
                fcq.MdocJustinNumberSet, fcq.PhysicalFileIdSet);
        }

        public async Task<List<RedactedCivilFileDetailResponse>> GetFilesByAgencyIdCodeAndFileNumberText(string location,
            string fileNumber, CourtLevelCd courtLevelCd)
        {
            var fileDetails = new List<RedactedCivilFileDetailResponse>();
            CourtClassCd courtClass = CourtClassCd.A;
            var courtClassSet = fileNumber.Contains("-") && Enum.TryParse(fileNumber.Split("-")[0], out courtClass);
            fileNumber = fileNumber.Contains("-") ? fileNumber.Split("-")[1] : fileNumber;

            var fileSearchResponse = await SearchAsync(new FilesCivilQuery
            {
                FileHomeAgencyId = location,
                FileNumber = fileNumber,
                SearchMode = SearchMode.FILENO,
                CourtLevel = courtLevelCd
            });

            if (fileSearchResponse.ResponseCd != "0")
                _logger.LogInformation("Civil search returned responseCd != 0");

            ValidUserHelper.CheckIfValidUser(fileSearchResponse.ResponseMessageTxt);

            var fileIdAndAppearanceDate = fileSearchResponse?.FileDetail?.Where(fd => !courtClassSet || fd.CourtClassCd == courtClass)
                                                           .SelectToList(fd => new { fd.PhysicalFileId, fd.NextApprDt });

            if (fileIdAndAppearanceDate == null || fileIdAndAppearanceDate.Count == 0)
                return fileDetails;

            //Return the basic entry without doing a lookup.
            if (fileIdAndAppearanceDate.Count == 1)
                return new List<RedactedCivilFileDetailResponse> { new RedactedCivilFileDetailResponse { PhysicalFileId = fileIdAndAppearanceDate.First().PhysicalFileId }} ;

            var fileDetailTasks = new List<Task<CivilFileDetailResponse>>();
            foreach (var fileId in fileIdAndAppearanceDate)
            {
                async Task<CivilFileDetailResponse> FileDetails() =>
                    await _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId.PhysicalFileId);
                fileDetailTasks.Add(_cache.GetOrAddAsync($"CivilFileDetail-{fileId}-{_requestAgencyIdentifierId}", FileDetails));
            }

            var fileDetailResponses = await fileDetailTasks.WhenAll();
            fileDetails = fileDetailResponses.SelectToList(fdr => _mapper.Map<RedactedCivilFileDetailResponse>(fdr));

            foreach (var fileDetail in fileDetails)
                fileDetail.NextApprDt = fileIdAndAppearanceDate.First(fa => fa.PhysicalFileId == fileDetail.PhysicalFileId)
                    .NextApprDt;

            return fileDetails;
        }

        public async Task<RedactedCivilFileDetailResponse> FileIdAsync(string fileId, bool isVcUser)
        {
            async Task<CivilFileDetailResponse> FileDetails() => await _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId);
            async Task<CivilFileContent> FileContent() => await _filesClient.FilesCivilFilecontentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, null, null, null, null, fileId);
            async Task<CivilAppearanceResponse> Appearances() => await PopulateDetailAppearancesAsync(FutureYN.Y, HistoryYN.Y, fileId);

            var fileDetailTask = _cache.GetOrAddAsync($"CivilFileDetail-{fileId}-{_requestAgencyIdentifierId}", FileDetails);
            var fileContentTask = _cache.GetOrAddAsync($"CivilFileContent-{fileId}-{_requestAgencyIdentifierId}", FileContent);
            var appearancesTask = _cache.GetOrAddAsync($"CivilAppearancesFull-{fileId}-{_requestAgencyIdentifierId}", Appearances);

            var fileDetail = await fileDetailTask;
            ValidUserHelper.CheckIfValidUser(fileDetail.ResponseMessageTxt);

            var appearances = await appearancesTask;
            var fileContent = await fileContentTask;

            if (fileDetail?.PhysicalFileId == null)
                return null;

            var detail = _mapper.Map<RedactedCivilFileDetailResponse>(fileDetail);
            foreach (var document in PopulateDetailCsrsDocuments(fileDetail.Appearance))
                if (!isVcUser)
                    detail.Document.Add(document);
   
            detail = await PopulateBaseDetail(detail);
            detail.Appearances = appearances;

            var fileContentCivilFile = fileContent?.CivilFile?.First(cf => cf.PhysicalFileID == fileId);
            detail.Party = await PopulateDetailParties(detail.Party);
            detail.Document = await PopulateDetailDocuments(detail.Document, fileContentCivilFile, isVcUser);
            detail.HearingRestriction = await PopulateDetailHearingRestrictions(fileDetail.HearingRestriction);
            if (isVcUser) { 
                //SCV-266 - Disable comments for VC Users.
                foreach (var document in detail.Document)
                    document.CommentTxt = "";
                detail.HearingRestriction = new List<CivilHearingRestriction>();
            }

            return detail;
        }

        public async Task<CivilAppearanceDetail> DetailedAppearanceAsync(string fileId, string appearanceId, bool isVcUser = false)
        {
            async Task<CivilFileDetailResponse> FileDetails() => await _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId);
            async Task<CivilFileContent> FileContent() => await _filesClient.FilesCivilFilecontentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, null, null, null, null, fileId);
            async Task<CivilFileAppearancePartyResponse> AppearanceParty() => await _filesClient.FilesCivilAppearancePartiesAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, appearanceId);
            async Task<CivilFileAppearanceApprMethodResponse> AppearanceMethods() => await _filesClient.FilesCivilAppearanceAppearancemethodsAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, appearanceId);
            async Task<CivilAppearanceResponse> Appearances() => await PopulateDetailAppearancesAsync(FutureYN.Y, HistoryYN.Y, fileId);

            var fileDetailTask = _cache.GetOrAddAsync($"CivilFileDetail-{fileId}-{_requestAgencyIdentifierId}", FileDetails);
            var appearancePartyTask = _cache.GetOrAddAsync($"CivilAppearanceParty-{fileId}-{appearanceId}-{_requestAgencyIdentifierId}", AppearanceParty);
            var fileContentTask = _cache.GetOrAddAsync($"CivilFileContent-{fileId}-{_requestAgencyIdentifierId}", FileContent);
            var appearanceMethodsTask = _cache.GetOrAddAsync($"CivilAppearanceMethods-{fileId}-{appearanceId}-{_requestAgencyIdentifierId}", AppearanceMethods);
            var appearancesTask = _cache.GetOrAddAsync($"CivilAppearancesFull-{fileId}-{_requestAgencyIdentifierId}", Appearances);

            var detail = await fileDetailTask;
            var appearances = await appearancesTask;
            var agencyId = await _locationService.GetLocationAgencyIdentifier(detail?.HomeLocationAgenId);
            var fileContent = await fileContentTask;

            var targetAppearance = appearances?.ApprDetail?.FirstOrDefault(app => app.AppearanceId == appearanceId);
            if (targetAppearance == null || detail == null)
                return null;

            //Sometimes we can have a bogus location, querying court list wont work.
            ClCivilCourtList civilCourtList = null;
            if (agencyId != null)
            {
                async Task<CourtList> CourtList() => await _filesClient.FilesCourtlistAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, agencyId, targetAppearance.CourtRoomCd, targetAppearance.AppearanceDt, "CV", detail.FileNumberTxt);
                var courtListTask = _cache.GetOrAddAsync($"CivilCourtList-{agencyId}-{targetAppearance.CourtRoomCd}-{targetAppearance.AppearanceDt}-{detail.FileNumberTxt}-{_requestAgencyIdentifierId}", CourtList);
                var courtList = await courtListTask;
                civilCourtList = courtList.CivilCourtList.FirstOrDefault(cl => cl.AppearanceId == appearanceId);
            }

            var appearanceParty = await appearancePartyTask;
            var appearanceMethodsResponse = await appearanceMethodsTask;
            var appearanceMethods = appearanceMethodsResponse.AppearanceMethod;

            var appearanceDetail = appearances.ApprDetail?.FirstOrDefault(app => app.AppearanceId == appearanceId);
            var fileDetailDocuments = detail.Document.Where(doc => doc.Appearance != null && doc.Appearance.Any(app => app.AppearanceId == appearanceId)).ToList();
            var fileContentCivilFile = fileContent?.CivilFile?.FirstOrDefault(cf => cf.PhysicalFileID == fileId);
            var previousAppearance = fileContentCivilFile?.PreviousAppearance.FirstOrDefault(pa => pa?.AppearanceId == appearanceId);

            var documents = await PopulateDetailedAppearanceDocuments(fileDetailDocuments, isVcUser);
            if (isVcUser)
            {
                foreach (var document in documents)
                    document.CommentTxt = "";
            }

            var detailedAppearance = new CivilAppearanceDetail
            {
                PhysicalFileId = fileId,
                AgencyId = await _locationService.GetLocationAgencyIdentifier(detail.HomeLocationAgenId),
                AppearanceId = appearanceId,
                AppearanceResultCd = appearanceDetail?.AppearanceResultCd,
                AppearanceReasonCd = appearanceDetail?.AppearanceReasonCd,
                AppearanceReasonDesc = await _lookupService.GetCivilAppearanceReasonsDescription(appearanceDetail?.AppearanceReasonCd),
                AppearanceResultDesc = await _lookupService.GetCivilAppearanceResultsDescription(appearanceDetail?.AppearanceResultCd),
                CourtRoomCd = targetAppearance.CourtRoomCd,
                FileNumberTxt = detail.FileNumberTxt,
                AppearanceDt = targetAppearance.AppearanceDt,
                AppearanceMethod = await PopulateAppearanceMethods(appearanceMethods),
                Party = await PopulateDetailedAppearancePartiesAsync(appearanceParty.Party, civilCourtList?.Parties, previousAppearance, appearanceMethods),
                Document = documents,
                Adjudicator = await PopulateDetailedAppearanceAdjudicator(previousAppearance, appearanceMethods),
                //AdjudicatorComment = previousAppearance?.AdjudicatorComment,
                CourtLevelCd = detail.CourtLevelCd
            };

            return detailedAppearance;
        }

        public async Task<JustinReportResponse> CourtSummaryReportAsync(string appearanceId, string reportName)
        {
            var justinReportResponse = await _filesClient.FilesCivilCourtsummaryreportAsync(_requestAgencyIdentifierId,
                _requestPartId, _applicationCode, appearanceId, reportName);
            return justinReportResponse;
        }

        public async Task<CivilFileContent> FileContentAsync(string agencyId, string roomCode, DateTime? proceeding, string appearanceId, string physicalFileId)
        {
            var proceedingDateString = proceeding.HasValue ? proceeding.Value.ToString("yyyy-MM-dd") : "";
            return await _filesClient.FilesCivilFilecontentAsync(_requestAgencyIdentifierId,
                _requestPartId, _applicationCode, agencyId, roomCode, proceedingDateString,
                appearanceId, physicalFileId);
        }

        #endregion Methods

        #region Helpers

        #region Civil Details

        private async Task<CivilAppearanceResponse> PopulateDetailAppearancesAsync(FutureYN? future, HistoryYN? history, string fileId)
        {
            var civilFileAppearancesResponse = await _filesClient.FilesCivilAppearancesAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, future, history,
                fileId);
            if (civilFileAppearancesResponse == null)
                return null;

            var civilAppearances = _mapper.Map<CivilAppearanceResponse>(civilFileAppearancesResponse);
            foreach (var appearance in civilAppearances.ApprDetail)
            {
                appearance.AppearanceReasonDsc = await _lookupService.GetCivilAppearanceReasonsDescription(appearance.AppearanceReasonCd);
                appearance.AppearanceResultDsc = await _lookupService.GetCivilAppearanceResultsDescription(appearance.AppearanceResultCd);
                appearance.AppearanceStatusDsc = await _lookupService.GetCivilAppearanceStatusDescription(appearance.AppearanceStatusCd.ToString());
                appearance.CourtLocationId = await _locationService.GetLocationAgencyIdentifier(appearance.CourtAgencyId);
                appearance.CourtLocation = await _locationService.GetLocationName(appearance.CourtAgencyId);
                appearance.DocumentTypeDsc = await _lookupService.GetDocumentDescriptionAsync(appearance.DocumentTypeCd);
            }
            return civilAppearances;
        }

        private IEnumerable<CivilDocument> PopulateDetailCsrsDocuments(ICollection<CvfcAppearance> appearances)
        {
            //Add in CSRs.
            return appearances.Select(appearance => new CivilDocument()
            {
                Category = "CSR",
                DocumentTypeDescription = "Court Summary",
                CivilDocumentId = appearance.AppearanceId,
                ImageId = appearance.AppearanceId,
                DocumentTypeCd = "CSR",
                LastAppearanceDt = appearance.AppearanceDate,
                FiledDt = appearance.AppearanceDate,
            });
        }

        private async Task<RedactedCivilFileDetailResponse> PopulateBaseDetail(RedactedCivilFileDetailResponse detail)
        {
            detail.HomeLocationAgencyCode = await _locationService.GetLocationAgencyIdentifier(detail.HomeLocationAgenId);
            detail.HomeLocationAgencyName = await _locationService.GetLocationName(detail.HomeLocationAgenId);
            detail.HomeLocationRegionName = await _locationService.GetRegionName(detail.HomeLocationAgencyCode);
            detail.CourtClassDescription = await _lookupService.GetCourtClassDescription(detail.CourtClassCd.ToString());
            detail.CourtLevelDescription = await _lookupService.GetCourtLevelDescription(detail.CourtLevelCd.ToString());
            detail.ActivityClassCd = await _lookupService.GetActivityClassCdLong(detail.CourtClassCd.ToString());
            detail.ActivityClassDesc = await _lookupService.GetActivityClassCdShort(detail.CourtClassCd.ToString());
            //Some lookups have LongDesc and ShortDesc the same. 
            if (detail.ActivityClassCd == detail.ActivityClassDesc)
                detail.ActivityClassCd = detail?.CourtClassCd.ToString();
            return detail;
        }

        private async Task<IList<CivilDocument>> PopulateDetailDocuments(IList<CivilDocument> documents, CvfcCivilFile civilFileContent, bool isVcUser)
        {
            //Populate extra fields for document.
            documents = documents.WhereToList(doc => !isVcUser || !_filterOutDocumentTypes.Contains(doc.DocumentTypeCd));
            foreach (var document in documents.Where(doc => doc.Category != "CSR"))
            {
                var documentFromFileContent = civilFileContent?.Document?.FirstOrDefault(doc => doc.DocumentId == document.CivilDocumentId);
                document.FiledBy = documentFromFileContent?.FiledBy;
                document.Category = _lookupService.GetDocumentCategory(document.DocumentTypeCd);
                document.DocumentTypeDescription = await _lookupService.GetDocumentDescriptionAsync(document.DocumentTypeCd);
                document.ImageId = document.SealedYN != "N" ? null : document.ImageId;
                document.NextAppearanceDt = document.Appearance?.Where(app => DateTime.TryParse(app?.AppearanceDate, out DateTime appearanceDate) && appearanceDate >= DateTime.Today).FirstOrDefault()?.AppearanceDate;
                document.Appearance = null;
                document.SwornByNm = documentFromFileContent.SwornByNm;
                document.AffidavitNo = documentFromFileContent.AffidavitNo;
                foreach (var issue in document.Issue)
                {
                    issue.IssueTypeDesc = await _lookupService.GetCivilDocumentIssueType(issue.IssueTypeCd);
                }
            }
            return documents;
        }

        private async Task<ICollection<CivilParty>> PopulateDetailParties(ICollection<CivilParty> parties)
        {
            //Populate extra fields for party.
            foreach (var party in parties)
                party.RoleTypeDescription = await _lookupService.GetCivilRoleTypeDescription(party.RoleTypeCd);
            return parties;
        }

        private async Task<ICollection<CivilHearingRestriction>> PopulateDetailHearingRestrictions(ICollection<CvfcHearingRestriction2> hearingRestrictions)
        {
            //TODO need permission for this filter.
            var civilHearingRestrictions = _mapper.Map<ICollection<CivilHearingRestriction>>(hearingRestrictions.Where(hr =>
                    hr.HearingRestrictionTypeCd == CvfcHearingRestriction2HearingRestrictionTypeCd.S)
                .ToList());

            foreach (var hearing in civilHearingRestrictions)
            {
                hearing.HearingRestrictionTypeDsc = await _lookupService.GetHearingRestrictionDescription(hearing.HearingRestrictionTypeCd.ToString());
            }
            return civilHearingRestrictions;
        }

        #endregion Civil Details

        #region Civil Appearance Details

        private async Task<ICollection<CivilAppearanceMethod>> PopulateAppearanceMethods(ICollection<JCCommon.Clients.FileServices.CivilAppearanceMethod> baseAppearanceMethods)
        {
            var appearanceMethods = _mapper.Map<ICollection<CivilAppearanceMethod>>(baseAppearanceMethods);
            foreach (var appearanceMethod in appearanceMethods)
            {
                appearanceMethod.AppearanceMethodDesc = await _lookupService.GetCivilAssetsDescription(appearanceMethod.AppearanceMethodCd);
                appearanceMethod.RoleTypeDesc = await _lookupService.GetCivilRoleTypeDescription(appearanceMethod.RoleTypeCd);
            }

            return appearanceMethods;
        }

        private async Task<CivilAdjudicator> PopulateDetailedAppearanceAdjudicator(CvfcPreviousAppearance previousAppearance, ICollection<JCCommon.Clients.FileServices.CivilAppearanceMethod> civilAppearanceMethods)
        {
            if (previousAppearance == null)
                return null;

            var appearanceMethodCd = civilAppearanceMethods.FirstOrDefault(am => am.RoleTypeCd == "ADJ")?.AppearanceMethodCd;
            return new CivilAdjudicator
            {
                FullName = previousAppearance.AdjudicatorName,
                AdjudicatorAppearanceMethod = previousAppearance.AdjudicatorAppearanceMethod,
                AdjudicatorAppearanceMethodDesc = await _lookupService.GetCivilAssetsDescription(previousAppearance.AdjudicatorAppearanceMethod),
                AppearanceMethodCd = appearanceMethodCd,
                AppearanceMethodDesc = await _lookupService.GetCivilAssetsDescription(appearanceMethodCd)
            };
        }

        /// <summary>
        /// This is mostly based off of getAppearanceCivilParty and expands by court list and FileContent.
        /// </summary>
        private async Task<ICollection<CivilAppearanceDetailParty>> PopulateDetailedAppearancePartiesAsync(ICollection<CivilAppearanceParty> parties,
            ICollection<ClParty> courtListParties, CvfcPreviousAppearance previousAppearance,
            ICollection<JCCommon.Clients.FileServices.CivilAppearanceMethod> civilAppearanceMethods)
        {
            var resultParties = new List<CivilAppearanceDetailParty>();
            foreach (var partyGroup in parties.GroupBy(a => a.PartyId))
            {
                //Map over our primary values from party group.
                var party = _mapper.Map<CivilAppearanceDetailParty>(partyGroup.First());

                //Get our roles from getAppearanceCivilParty. These should essentially be the same.
                party.PartyRole = await partyGroup.Select(async pg => new ClPartyRole
                {
                    RoleTypeCd = pg.PartyRoleTypeCd,
                    RoleTypeDsc = await _lookupService.GetCivilRoleTypeDescription(pg.PartyRoleTypeCd)
                }).WhenAll();

                //Get information from appearanceMethods.
                if (civilAppearanceMethods.Any(am => party.PartyRole.Any(pr => pr.RoleTypeCd == am.RoleTypeCd)))
                {
                    party.AppearanceMethodCd = civilAppearanceMethods.First().AppearanceMethodCd;
                    party.AppearanceMethodDesc = await _lookupService.GetCivilAssetsDescription(civilAppearanceMethods.First().AppearanceMethodCd);
                }

                //Get the additional information from court list.
                var courtListParty = courtListParties?.FirstOrDefault(clp => clp.PartyId == partyGroup.Key);
                if (courtListParty != null)
                {
                    party.AttendanceMethodCd = courtListParty.AttendanceMethodCd;
                    party.AttendanceMethodDesc = await _lookupService.GetCivilAssetsDescription(party.AttendanceMethodCd);
                    party.Counsel = _mapper.Map<ICollection<CivilCounsel>>(courtListParty.Counsel);
                    party.Representative = _mapper.Map<ICollection<CivilRepresentative>>(courtListParty.Representative);
                    foreach (var representative in party.Representative)
                    {
                        representative.AttendanceMethodDesc = await _lookupService.GetCivilAssetsDescription(representative.AttendanceMethodCd); 
                    }
                    party.LegalRepresentative = courtListParty.LegalRepresentative;
                }

                //Add additional information from previous appearance - this comes from FileContent.
                if (previousAppearance?.CourtParticipant != null && previousAppearance.CourtParticipant.Any(cp => cp.PartId == party.PartyId))
                {
                    //Can be multiple rows here, but they repeat for the partyRole.
                    var targetParticipant = previousAppearance.CourtParticipant.First(cp => cp.PartId == party.PartyId);
                    party.PartyAppearanceMethod = targetParticipant.PartyAppearanceMethod;
                    party.PartyAppearanceMethodDesc = await _lookupService.GetCivilPartyAttendanceType(targetParticipant.PartyAppearanceMethod);

                    //Update the counsel with their appearanceMethod.
                    foreach (var counsel in targetParticipant.Counsel)
                    {
                        //We match on counselName. 
                        //Not the best idea of matching data, but we aren't provided a counselId for Civil FileContent.  Although we are provided a counselId in FileDetails, CourtList. 
                        //TEST Environment - Civil Case 2151 - Appearance 9042, demonstrates this.

                        if (!counsel.AdditionalProperties.ContainsKey("counselName"))
                            continue;
                  
                        var targetCounsel = party.Counsel?.FirstOrDefault(c => c.CounselFullName == counsel.CounselName);
                        if (targetCounsel == null)
                            continue;

                        targetCounsel.CounselAppearanceMethod = counsel.CounselAppearanceMethod;
                        targetCounsel.CounselAppearanceMethodDesc = await _lookupService.GetCivilCounselAttendanceType(targetCounsel.CounselAppearanceMethod);
                    }
                }

                resultParties.Add(party);
            }
            return resultParties;
        }

        private async Task<ICollection<CivilAppearanceDocument>> PopulateDetailedAppearanceDocuments(List<CvfcDocument3> fileDetailDocuments, bool isVcUser)
        {
            //CivilAppearanceDocument, doesn't include appearances.
            var documents = _mapper.Map<ICollection<CivilAppearanceDocument>>(fileDetailDocuments);
            documents = documents.WhereToList(doc => !isVcUser || !_filterOutDocumentTypes.Contains(doc.DocumentTypeCd));
            foreach (var document in documents)
            {
                document.Category = _lookupService.GetDocumentCategory(document.DocumentTypeCd);
                document.DocumentTypeDescription = await _lookupService.GetDocumentDescriptionAsync(document.DocumentTypeCd);
				document.ImageId = document.SealedYN != "N" ? null : document.ImageId;
                foreach (var issue in document.Issue)
                {
                    issue.IssueTypeDesc = await _lookupService.GetCivilDocumentIssueType(issue.IssueTypeCd);
                    issue.IssueResultCdDesc = await _lookupService.GetCivilDocumentIssueResult(issue.IssueResultCd);
                }
            }
            return documents;
        }

        #endregion Civil Appearance Details

        #endregion Helpers
    }
}