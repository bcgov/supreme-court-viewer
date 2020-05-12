using System;
using System.Linq;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LookupServices;
using JCCommon.Models;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using tests.api.Helpers;
using Xunit;

namespace tests.api.Controllers
{
    /// <summary>
    /// These tests, ensure Api.FilesController and JC-Client-Interface.FileServicesClient work correctly. 
    /// Credit to DARS for most of these tests. 
    /// </summary>
    public class FilesControllerTests
    {
        #region Variables
        private readonly FilesController _controller;
        #endregion

        #region Constructor
        public FilesControllerTests()
        {
            var preTest = new EnvironmentBuilder("FileServicesClient:Username", "FileServicesClient:Password", typeof(FilesController));
            var lookupServiceClient = new LookupServiceClient(preTest.HttpClient);
            var fileServicesClient = new FileServicesClient(preTest.HttpClient);
            var lookupService = new LookupService(preTest.Configuration, lookupServiceClient, new CachingService());
            var filesService = new FilesService(preTest.Configuration, fileServicesClient, new Mapper(), lookupService );
            _controller = new FilesController(preTest.Configuration, preTest.LogFactory.CreateLogger<FilesController>(), filesService);
            SetupMocks();
        }
        #endregion

        #region Tests
        [Fact]
        public async void Criminal_File_Search_By_LastName()
        {
            var fcq = new FilesCriminalQuery()
            {
                SearchMode = SearchMode.PARTNAME,
                FileHomeAgencyId = "83.0001",
                LastName = "Sm"
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(fileSearchResponse.FileDetail, fd => fd.Participant.Any(p => p.FullNm.Contains("Smith")));
        }

        [Fact]
        public async void Civil_File_Search_By_LastName()
        {
            var fcq = new FilesCivilQuery()
            {
                SearchMode = SearchMode2.PARTNAME,
                FileHomeAgencyId = "83.0001",
                LastName = "bad",
                CourtLevel = CourtLevelCd3.P
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Contains("2437", fileSearchResponse.FileDetail.First().PhysicalFileId);
            Assert.Contains("BADGUY, Borris", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Criminal_File_Search_By_JustinNo()
        {
            var fcq = new FilesCriminalQuery()
            {
                SearchMode = SearchMode.JUSTINNO,
                FileHomeAgencyId = "83.00001",
                MdocJustinNoSet = "35674"
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(fileSearchResponse.FileDetail, fd => fd.MdocJustinNo == "35674");
        }

        [Fact]
        public async void Civil_File_Search_By_PhysicalFileId()
        {
            var fcq = new FilesCivilQuery()
            {
                SearchMode = SearchMode2.PHYSID,
                FileHomeAgencyId = "83.0001",
                PhysicalFileIdSet = "2506"
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Equal("C-11011", fileSearchResponse.FileDetail.First().FileNumberTxt);
            Assert.Contains("BYSTANDER, Innocent", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Criminal_File_Details_by_JustinNo()
        {
            var actionResult = await _controller.GetCriminalFileDetailByFileId("35674");

            var redactedCriminalFileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("35674", redactedCriminalFileDetailResponse.JustinNo);
            Assert.True(redactedCriminalFileDetailResponse.Participant.Count > 0);
        }

        [Fact]
        public async void Civil_File_Details_by_PhysicalFileId()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("40");

            var redactedCivilFileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("40", redactedCivilFileDetailResponse.PhysicalFileId);
            Assert.Equal("P-241", redactedCivilFileDetailResponse.FileNumberTxt);
            Assert.Contains(redactedCivilFileDetailResponse.Party, f => f.LastNm == "Kings");
            Assert.Contains(redactedCivilFileDetailResponse.Party, f => f.LastNm == "Jones");
        }

        [Fact]
        public async void Criminal_Appearances_by_JustinNo()
        {
            var actionResult = await _controller.GetCriminalAppearancesByFileId("35674");

            var criminalFileAppearancesResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(criminalFileAppearancesResponse.ApprDetail,
                f => f.LastNm == "Young" && f.GivenNm == "Johnny");
        }

        [Fact]
        public async void Civil_Appearances_by_PhysicalFileId()
        {
            var actionResult = await _controller.GetCivilAppearancesByFileId("2506", FutureYN2.Y, HistoryYN2.Y);

            var civilFileAppearancesByFileId = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("0", civilFileAppearancesByFileId.FutureRecCount);
            Assert.Equal("20", civilFileAppearancesByFileId.HistoryRecCount);
        }

        [Fact]
        public async void Criminal_Appearances_by_JustinNo_0()
        {
            var actionResult = await _controller.GetCriminalAppearancesByFileId("0");

            var criminalFileAppearancesResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("0", criminalFileAppearancesResponse.FutureRecCount);
            Assert.Equal("0", criminalFileAppearancesResponse.HistoryRecCount);
        }

        [Fact]
        public async void GetCourtlist()
        {
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2016-04-04"), "CR", "4889-1");

            var criminalFileAppearancesResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", criminalFileAppearancesResponse.CourtLocationName);
            Assert.Equal("101", criminalFileAppearancesResponse.CourtRoomCode);
            Assert.Equal("2016-04-04", criminalFileAppearancesResponse.CourtProceedingsDate);
        }

        [Fact]
        public async void Criminal_File_Content()
        {
            var actionResult = await _controller.GetCriminalFileContent("4801", "101", DateTime.Parse("2016-04-04"), "44150.0734");

            var criminalFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801",criminalFileContent.CourtLocaCd);
            Assert.Equal("101", criminalFileContent.CourtRoomCd);
            Assert.Equal("2016-04-04", criminalFileContent.CourtProceedingDate);
        }

        [Fact]
        public async void Civil_File_Content_By_AgencyId_Room_Proceeding_Appearance()
        {
            var actionResult = await _controller.GetCivilFileContent("4801", "101", DateTime.Parse("2016-04-04"), "984");

            var civilFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", civilFileContent.CourtLocaCd);
            Assert.Equal("101", civilFileContent.CourtRoomCd);
            Assert.Equal("2016-04-04", civilFileContent.CourtProceedingDate);
            Assert.Equal(1, civilFileContent.CivilFile.Count);
            Assert.Equal("2506", civilFileContent.CivilFile.First().PhysicalFileID);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_Provincial()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
                CourtLevel = CourtLevelCd2.P
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("2", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_Supreme()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
                CourtLevel = CourtLevelCd2.S
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_ProvincialAndSupreme()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("3", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Civil_File_Search_By_FileNo_Provincial()
        {
            var fcq = new FilesCivilQuery
            {
                SearchMode = SearchMode2.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumber =  "11011",
                CourtLevel =  CourtLevelCd3.P
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Equal("2506", fileSearchResponse.FileDetail.First().PhysicalFileId);
            Assert.Contains("BYSTANDER, Innocent", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Civil_Court_Summary_Report()
        {
            var actionResult = await _controller.GetCivilCourtSummaryReport("984","test123.pdf");

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.Equal(5139,fileContentResult.FileContents.Length);
        }

        [Fact]
        public async void Criminal_Record_Of_Proceeding()
        {
            var actionResult = await _controller.GetRecordsOfProceeding("12971.0026", "ropTest.pdf", "24", CourtLevelCd.P , CourtClassCd.A);

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.Equal(79161, fileContentResult.FileContents.Length);
        }


        [Fact]
        public async void Civil_File_Content_By_FileId()
        {
            var actionResult = await _controller.GetCivilFileContent(physicalFileId: "2506");

            var civilFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Null(civilFileContent.CourtLocaCd);
            Assert.Null(civilFileContent.CourtRoomCd);
            Assert.Equal("", civilFileContent.CourtProceedingDate);
            Assert.Equal(1, civilFileContent.CivilFile.Count);
            Assert.Equal("2506", civilFileContent.CivilFile.First().PhysicalFileID);
        }

        [Fact]
        public async void Document_Civil()
        {
            var actionResult = await _controller.GetDocument("10010", "test.pdf");

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.Equal(14625, fileContentResult.FileContents.Length);
        }

        [Fact]
        public async void Document_Criminal()
        {
            var actionResult = await _controller.GetDocument(documentId: "40","test.pdf", true);

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.Equal(782434, fileContentResult.FileContents.Length);
        }

        [Fact]
        public async void Criminal_File_Content_By_JustinNumber()
        {
            var actionResult = await _controller.GetCriminalFileContent(justinNumber: "3179.0000");

            var criminalFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("", criminalFileContent.CourtLocaCd);
            Assert.Equal("", criminalFileContent.CourtRoomCd);
            Assert.Equal("", criminalFileContent.CourtProceedingDate);
            Assert.Equal(1, criminalFileContent.AccusedFile.Count);
            Assert.Equal("3179", criminalFileContent.AccusedFile.First().MdocJustinNo);
        }
        #endregion

        #region Helpers

        private void SetupMocks()
        {
            var headerDictionary = new HeaderDictionary();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Headers).Returns(headerDictionary);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(a => a.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };

        }
        #endregion
    }
}
