﻿using JCCommon.Clients.FileServices;
using JCCommon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Constants;
using Scv.Api.Helpers.Exceptions;
using Scv.Api.Models.Civil.Detail;
using Scv.Api.Models.Criminal.Detail;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Services.Files;
using CivilAppearanceDetail = Scv.Api.Models.Civil.AppearanceDetail.CivilAppearanceDetail;
using CriminalAppearanceDetail = Scv.Api.Models.Criminal.AppearanceDetail.CriminalAppearanceDetail;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.archive;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]

    public class FilesController : ControllerBase
    {
        #region Variables

        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private readonly FilesService _filesService;
        private readonly CivilFilesService _civilFilesService;
        private readonly CriminalFilesService _criminalFilesService;
        private readonly VcCivilFileAccessHandler _vcCivilFileAccessHandler;

        #endregion Variables

        #region Constructor

        public FilesController(IConfiguration configuration, ILogger<FilesController> logger, FilesService filesService, VcCivilFileAccessHandler vcCivilFileAccessHandler)
        {
            _configuration = configuration;
            _logger = logger;
            _filesService = filesService;
            _civilFilesService = filesService.Civil;
            _criminalFilesService = filesService.Criminal;
            _vcCivilFileAccessHandler = vcCivilFileAccessHandler;
        }

        #endregion Constructor

        #region Actions

        #region Civil Only

        /// <summary>
        /// Provides facilities for performing a civil file search.
        /// </summary>
        /// <param name="fcq">FileCivilQuery object with many parameters - Search Modes: FILENO = 0, PARTNAME = 1, CROWN = 2, JUSTINNO = 3, PHYSID = 4</param>
        /// 
        /// <returns>FileSearchResponse</returns>
        [HttpPost]
        [Route("civil/search")]
        public async Task<ActionResult<FileSearchResponse>> FilesCivilSearchAsync(FilesCivilQuery fcq)
        {
            var fileSearchResponse = await _civilFilesService.SearchAsync(fcq);
            return Ok(fileSearchResponse);
        }

        /// <summary>
        /// Gets the details for a given a location and civil file number text.
        /// </summary>
        /// <param name="location">Agency Location Id Code: EX. 104.0001</param>
        /// <param name="fileNumber">FileNumber: EX. P-241</param>
        /// <returns>List{RedactedCivilFileDetailResponse}</returns>
        [HttpGet]
        [Route("civil")]
        public async Task<ActionResult<List<RedactedCivilFileDetailResponse>>> GetCivilFileIdsByAgencyIdCodeAndFileNumberText(string location, string fileNumber)
        {
            var civilFiles = await _civilFilesService.GetFilesByAgencyIdCodeAndFileNumberText(location, fileNumber);
            if (civilFiles == null || civilFiles.Count == 0)
                throw new NotFoundException("Couldn't find civil file with this location and file number.");

            return Ok(civilFiles);
        }

        /// <summary>
        /// Gets the details for a given civil file id.
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns>RedactedCivilFileDetailResponse</returns>
        [HttpGet]
        [Route("civil/{fileId}")]
        public async Task<ActionResult<RedactedCivilFileDetailResponse>> GetCivilFileDetailByFileId(string fileId)
        {
            if (User.IsVcUser() && !await _vcCivilFileAccessHandler.HasCivilFileAccess(User, fileId))
                return Forbid();

            var civilFileDetailResponse = await _civilFilesService.FileIdAsync(fileId);
            if (civilFileDetailResponse?.PhysicalFileId == null)
                throw new NotFoundException("Couldn't find civil file with this id.");

            if (User.IsVcUser() && civilFileDetailResponse.SealedYN == "Y")
                return Forbid();

            return Ok(civilFileDetailResponse);
        }

        /// <summary>
        /// Gets detailed information regarding an appearance given civil file id and appearance id.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="appearanceId"></param>
        /// <returns>CivilAppearanceDetail</returns>
        [HttpGet]
        [Route("civil/{fileId}/appearance-detail/{appearanceId}")]
        public async Task<ActionResult<CivilAppearanceDetail>> GetCivilAppearanceDetails(string fileId, string appearanceId)
        {
            if (User.IsVcUser())
            {
                if(!await _vcCivilFileAccessHandler.HasCivilFileAccess(User, fileId))
                    return Forbid();

                var civilFileDetailResponse = await _civilFilesService.FileIdAsync(fileId);
                if (civilFileDetailResponse?.PhysicalFileId == null)
                    throw new NotFoundException("Couldn't find civil file with this id.");
                if (civilFileDetailResponse.SealedYN == "Y")
                    return Forbid();
            } 

            var civilAppearanceDetail = await _civilFilesService.DetailedAppearanceAsync(fileId, appearanceId);
            if (civilAppearanceDetail == null)
                throw new NotFoundException("Couldn't find appearance detail with the provided file id and appearance id.");
            return Ok(civilAppearanceDetail);
        }

        /// <summary>
        /// Gets court summary report for a given appearance id.
        /// </summary>
        /// <param name="appearanceId"></param>
        /// <param name="fileNameAndExtension"></param>
        /// <param name="vcCivilFileId"></param>
        /// <returns>JustinReportResponse</returns>
        [HttpGet]
        [Route("civil/court-summary-report/{appearanceId}/{fileNameAndExtension}")]
        public async Task<IActionResult> GetCivilCourtSummaryReport(string appearanceId, string fileNameAndExtension, string vcCivilFileId = "")
        {
            if (User.IsVcUser())
            {
                if (!await _vcCivilFileAccessHandler.HasCivilFileAccess(User, vcCivilFileId))
                    return Forbid();

                var civilFileDetailResponse = await _civilFilesService.FileIdAsync(vcCivilFileId);
                if (civilFileDetailResponse?.PhysicalFileId == null)
                    throw new NotFoundException("Couldn't find civil file with this id.");
                if (civilFileDetailResponse.SealedYN == "Y" || civilFileDetailResponse.Appearances.ApprDetail.All(ad => ad.AppearanceId != appearanceId))
                    return Forbid();
            }

            var justinReportResponse = await _civilFilesService.CourtSummaryReportAsync(appearanceId, JustinReportName.CEISR035);

            if (justinReportResponse.ReportContent == null || justinReportResponse.ReportContent.Length <= 0)
                throw new NotFoundException("Couldn't find CSR with this appearance id.");

            return BuildPdfFileResponse(justinReportResponse.ReportContent);
        }

        /// <summary>
        /// Gets the civil file content.
        /// </summary>
        /// <param name="agencyId"></param>
        /// <param name="roomCode"></param>
        /// <param name="proceeding">DateTime? converted to yyyy-MM-dd string</param>
        /// <param name="appearanceId"></param>
        /// <param name="physicalFileId"></param>
        /// <returns>CivilFileContent</returns>
        [HttpGet]
        [Route("civil/file-content")]
        public async Task<ActionResult<CivilFileContent>> GetCivilFileContent(string agencyId = null, string roomCode = null, DateTime? proceeding = null, string appearanceId = null, string physicalFileId = "")
        {
            var civilFileContent = await _civilFilesService.FileContentAsync(agencyId, roomCode, proceeding, appearanceId, physicalFileId);
            return Ok(civilFileContent);
        }

        #endregion Civil Only

        #region Criminal Only

        /// <summary>
        /// Provides facilities for performing a criminal file search.
        /// </summary>
        /// <param name="fcq">FileCriminalQuery, composed of parameters for search. </param>
        /// <returns>FileSearchResponse</returns>
        [HttpPost]
        [Route("criminal/search")]
        public async Task<ActionResult<FileSearchResponse>> FilesCriminalSearchAsync(FilesCriminalQuery fcq)
        {
            var fileSearchResponse = await _criminalFilesService.SearchAsync(fcq);
            return Ok(fileSearchResponse);
        }

        /// <summary>
        /// Gets the details for a given a location and criminal file number text.
        /// </summary>
        /// <param name="location"> Agency Identifier Code to look for EX. 83.0001</param>
        /// <param name="fileNumber"> FileNumberText to look for EX. 500-2</param>
        /// <returns>List{RedactedCriminalFileDetailResponse}</returns>
        [HttpGet]
        [Route("criminal")]
        public async Task<ActionResult<List<RedactedCriminalFileDetailResponse>>> GetCriminalFileIdsByAgencyIdCodeAndFileNumberText(string location, string fileNumber)
        {
            var criminalFiles = await _criminalFilesService.GetFilesByAgencyIdCodeAndFileNumberText(location, fileNumber);
            if (criminalFiles == null || criminalFiles.Count == 0)
                throw new NotFoundException("Couldn't find criminal file with this location and file number.");

            return Ok(criminalFiles);
        }

        /// <summary>
        /// Get details for a given file id.
        /// </summary>
        /// <param name="fileId">Target file id.</param>
        /// <returns>CriminalFileDetailResponse</returns>
        [HttpGet]
        [Route("criminal/{fileId}")]
        public async Task<ActionResult<RedactedCriminalFileDetailResponse>> GetCriminalFileDetailByFileId(string fileId)
        {
            var redactedCriminalFileDetailResponse = await _criminalFilesService.FileIdAsync(fileId);
            if (redactedCriminalFileDetailResponse?.JustinNo == null)
                throw new NotFoundException("Couldn't find criminal file with this id.");
            return Ok(redactedCriminalFileDetailResponse);
        }

        /// <summary>
        /// Gets detailed information regarding an appearance given criminal file id, appearance id, participant id.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="appearanceId"></param>
        /// <param name="partId"></param>
        /// <returns>CriminalAppearanceDetail</returns>
        [HttpGet]
        [Route("criminal/{fileId}/appearance-detail/{appearanceId}/{partId}")]
        public async Task<ActionResult<CriminalAppearanceDetail>> GetCriminalAppearanceDetails(string fileId, string appearanceId, string partId)
        {
            var appearanceDetail = await _criminalFilesService.AppearanceDetailAsync(fileId, appearanceId, partId);
            if (appearanceDetail == null)
                throw new NotFoundException("Couldn't find appearance details with the provided parameters.");
            return Ok(appearanceDetail);
        }

        /// <summary>
        /// Gets the criminal file content.
        /// </summary>
        /// <param name="agencyId"></param>
        /// <param name="roomCode"></param>
        /// <param name="proceeding">The proceeding date in the format YYYY-MM-dd</param>
        /// <param name="appearanceId"></param>
        /// <param name="justinNumber"></param>
        /// <returns>CriminalFileContent</returns>
        [HttpGet]
        [Route("criminal/file-content")]
        public async Task<ActionResult<CriminalFileContent>> GetCriminalFileContent(string agencyId = null, string roomCode = null, DateTime? proceeding = null, string appearanceId = null, string justinNumber = null)
        {
            var criminalFileContent = await _criminalFilesService.FileContentAsync(agencyId, roomCode, proceeding, appearanceId, justinNumber);
            return Ok(criminalFileContent);
        }

        /// <summary>
        /// Gets records of proceedings.
        /// </summary>
        /// <param name="partId">The participant id associated to the Record Of Proceedings.</param>
        /// <param name="fileNameAndExtension"></param>
        /// <param name="profSequenceNumber"></param>
        /// <param name="courtLevelCode">The associated court level code. P = 0, S = 1, A = 2: Provincial, Supreme, All</param>
        /// <param name="courtClassCode">The associated court class code. A = 0, Y = 1, T = 2, F = 3, C = 4, M = 5, L = 6, R = 7, B = 8, D = 9, E = 10, G = 11, H = 12, N = 13, O = 14, P = 15, S = 16, V = 17</param>
        /// <returns>FileContentResult</returns>
        [HttpGet]
        [Route("criminal/record-of-proceedings/{partId}/{fileNameAndExtension}")]
        public async Task<IActionResult> GetRecordsOfProceeding(string partId, string fileNameAndExtension, string profSequenceNumber, CourtLevelCd courtLevelCode, CourtClassCd courtClassCode)
        {
            var recordsOfProceeding = await _criminalFilesService.RecordOfProceedingsAsync(partId, profSequenceNumber, courtLevelCode, courtClassCode);

            if (recordsOfProceeding.B64Content == null || recordsOfProceeding.B64Content.Length <= 0)
                throw new NotFoundException("Couldn't find ROP with this part id.");

            return BuildPdfFileResponse(recordsOfProceeding.B64Content);
        }


        #endregion Criminal Only

        #region Documents

        /// <summary>
        /// Gets a document.
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="fileNameAndExtension">Name of the file and extension.</param>
        /// <param name="isCriminal">True if Criminal, False if Civil</param>
        /// <param name="vcCivilFileId"></param>
        /// <returns>DocumentResponse</returns>
        [HttpGet]
        [Route("document/{documentId}/{fileNameAndExtension}")]
        public async Task<IActionResult> GetDocument(string documentId, string fileNameAndExtension, bool isCriminal = false, string vcCivilFileId = "")
        {
            documentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentId));
            if (User.IsVcUser())
            {
                if (!await _vcCivilFileAccessHandler.HasCivilFileAccess(User, vcCivilFileId))
                    return Forbid();

                if (isCriminal)
                    return Forbid();

                var civilFileDetailResponse = await _civilFilesService.FileIdAsync(vcCivilFileId);
                if (civilFileDetailResponse?.PhysicalFileId == null)
                    throw new NotFoundException("Couldn't find civil file with this id.");

                if (civilFileDetailResponse.SealedYN == "Y" || civilFileDetailResponse.Document.All(s => s.CivilDocumentId != documentId))
                    return Forbid();
            }

            var documentResponse = await _filesService.DocumentAsync(documentId, isCriminal);

            if (documentResponse.B64Content == null || documentResponse.B64Content.Length <= 0)
                throw new NotFoundException("Couldn't find document with this id.");

            return BuildPdfFileResponse(documentResponse.B64Content);
        }

        [HttpPost]
        [Route("archive")]
        public async Task<IActionResult> GetArchive(ArchiveRequest archiveRequest)
        {
            if (User.IsVcUser())
            {
                if (!await _vcCivilFileAccessHandler.HasCivilFileAccess(User, archiveRequest.VcCivilFileId))
                    return Forbid();

                if (archiveRequest.RopRequests.Any() || archiveRequest.DocumentRequests.Any(dr => dr.IsCriminal))
                    return Forbid();

                var civilFileDetailResponse = await _civilFilesService.FileIdAsync(archiveRequest.VcCivilFileId);
                if (civilFileDetailResponse?.PhysicalFileId == null)
                    throw new NotFoundException("Couldn't find civil file with this id.");

                var documentIds = archiveRequest.DocumentRequests.SelectToList(d => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(d.Base64UrlEncodedDocumentId)));
                var appearanceIds = archiveRequest.CsrRequests.SelectToList(csr => csr.AppearanceId);

                if (civilFileDetailResponse.SealedYN == "Y" || !documentIds.All(id => civilFileDetailResponse.Document.Any(d => d.CivilDocumentId == id))
                || !appearanceIds.All(id => civilFileDetailResponse.Appearances.ApprDetail.Any(d => d.AppearanceId == id))
                )
                    return Forbid();
            }

            var maximumArchiveDocumentCount = _configuration.GetNonEmptyValue("MaximumArchiveDocumentCount");
            if (string.IsNullOrEmpty(archiveRequest.ZipName)) return BadRequest($"Missing {nameof(archiveRequest.ZipName)}.");
            if (archiveRequest.TotalDocuments >= int.Parse(maximumArchiveDocumentCount))   return BadRequest($"Only can support up to {maximumArchiveDocumentCount} documents.");

            var courtSummaryRequests = archiveRequest.CsrRequests;
            var documentRequest = archiveRequest.DocumentRequests;
            var ropRequests = archiveRequest.RopRequests;

            var courtSummaryReportsTasks = courtSummaryRequests.Select(a => _civilFilesService.CourtSummaryReportAsync(
                a.AppearanceId,
                JustinReportName.CEISR035));
            var documentTasks = documentRequest.Select(d => _filesService.DocumentAsync(
                Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(d.Base64UrlEncodedDocumentId)),
                d.IsCriminal));
            var ropRequestTasks = ropRequests.SelectToList(dr =>
                _criminalFilesService.RecordOfProceedingsAsync(dr.PartId,
                    dr.ProfSequenceNumber,
                    dr.CourtLevelCode,
                    dr.CourtClassCode));

            var courtSummaryReports = (await courtSummaryReportsTasks.WhenAll()).ToList();
            var documents = (await documentTasks.WhenAll()).ToList();
            var rops = (await ropRequestTasks.WhenAll()).ToList();

            if (courtSummaryReports.Any(d => d.ResponseCd != "0")) return BadRequest("One of the CSRs didn't return correctly.");
            if (documents.Any(d => d.ResultCd == "0")) return BadRequest("One of the documents didn't return correctly.");
            if (rops.Any(d => d.ResultCd == "0")) return BadRequest("One of the ROPs didn't return correctly.");

            var pdfDocuments = courtSummaryReports.SelectToList(d => new PdfDocument
                { Content = d.ReportContent, FileName = courtSummaryRequests[courtSummaryReports.IndexOf(d)].PdfFileName });

            pdfDocuments.AddRange(documents.SelectToList(d => new PdfDocument
                { Content = d.B64Content, FileName = documentRequest[documents.IndexOf(d)].PdfFileName}));

            pdfDocuments.AddRange(rops.Select(d => new PdfDocument
                { Content = d.B64Content, FileName = ropRequests[rops.IndexOf(d)].PdfFileName }));

            return await BuildArchiveWithPdfFiles(pdfDocuments, archiveRequest.ZipName);
        }

        #endregion Documents 

        #region Helpers

        private async Task<FileContentResult> BuildArchiveWithPdfFiles(IEnumerable<PdfDocument> pdfDocuments, string zipName)
        {
            await using var outStream = new MemoryStream();
            using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
            {
                foreach (var document in pdfDocuments)
                {
                    var documentContent = Convert.FromBase64String(document.Content);
                    var documentName = document.FileName;
                    documentName = documentName.EndsWith(".pdf") ? documentName : $"{documentName}.pdf";

                    await using var entryStream = archive.CreateEntry(documentName, CompressionLevel.Optimal).Open();
                    await using var fileToCompressStream = new MemoryStream(documentContent);
                    fileToCompressStream.WriteTo(entryStream);
                }
            }
            return BuildZipFileResponse(outStream.ToArray(), zipName);
        }

        private FileContentResult BuildZipFileResponse(byte[] content, string name)
        {
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            name = name.EndsWith(".zip") ? name : $"{name}.zip";
            return File(content, "application/octet-stream", name);
        }

        private FileContentResult BuildPdfFileResponse(string content)
        {
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(Convert.FromBase64String(content), "application/pdf");
        }

        #endregion Helpers

        #endregion Actions
    }
}