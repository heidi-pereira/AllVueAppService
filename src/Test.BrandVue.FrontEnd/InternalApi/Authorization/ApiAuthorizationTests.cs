using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Models.ExcelExport;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.QuotaCells;
using NUnit.Framework;
using Vue.Common.Constants.Constants;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.InternalApi.Authorization
{
    internal class ApiAuthorizationTests
    {
        private static readonly SigDiffOptions sigOptions = new SigDiffOptions(
            true, 
            SigConfidenceLevel.NinetyFive, 
            DisplaySignificanceDifferences.ShowBoth, 
            CrosstabSignificanceType.CompareToTotal);

        private static readonly CuratedResultsModel RequestData = new(new DemographicFilter(new FilterRepository()),
            new[] {1},
            "UKSubset",
            new[] {"measure"},
            new Period(),
            1,
            null,
            sigOptions);

        private static readonly EntityInstanceRequest[] SingleInstance = { new (null, new []{0}) };

        private static readonly MultiEntityRequestModel MultiEntityRequestData = new("measure", "UKSubset", new Period(),
            new EntityInstanceRequest(null,
                Array.Empty<int>()),
                SingleInstance,
                new DemographicFilter(new FilterRepository()), 
                new CompositeFilterModel(), 
                Array.Empty<MeasureFilterRequestModel>(), 
                Array.Empty<BaseExpressionDefinition>(),
                false,
                SigConfidenceLevel.NinetyFive);

        private static readonly ExcelExportModel ExcelRequestData = new(RequestData, Array.Empty<string>(), "",
            ViewTypeEnum.Competition, "", Array.Empty<RequestMeasureForEntity>(), null, null, "QuestionText");

        private static readonly ExcelExportMultipleEntitiesModel MultiEntityExcelRequestData = new(MultiEntityRequestData, Array.Empty<string>(), "",
            ViewTypeEnum.Competition, "", Array.Empty<RequestMeasureForEntity>(), null, null, "QuestionText");

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsForbidden_WhenCompanyProductsMismatch()
        {
            await InternalBrandVueApi
                .WithAccessToProducts("someproduct".Yield().ToArray())
                .PostAsyncAssert("api/data/overtimemultipleentity", MultiEntityRequestData, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsForbidden_WhenRequiredClaimsNotPresent()
        {
            var allRequiredClaimsButOne = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                RequiredClaims.UserId,
                RequiredClaims.Username,
                RequiredClaims.FirstName,
                RequiredClaims.LastName,
                RequiredClaims.Role,
                RequiredClaims.Products,
                RequiredClaims.Subsets,
                RequiredClaims.CurrentCompanyShortCode,
            };

            await InternalBrandVueApi
                .WithOnlyTheseClaimTypes(allRequiredClaimsButOne)
                .PostAsyncAssert("api/data/overtimemultipleentity", MultiEntityRequestData, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Test_CuratedResultsRequest_ReturnsForbidden_WhenTrialUserAndTrialEndDateInThePast()
        {
            await InternalBrandVueApi
                .WithRole(Roles.TrialUser)
                .WithTrialEndDate(DateTimeOffset.Parse("2020-01-01"))
                .PostAsyncAssert("api/data/overtimemultipleentity", MultiEntityRequestData, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Test_ExcelExportRequest_ReturnsForbidden_WhenTrialUser()
        {
            await InternalBrandVueApi
                .WithRole(Roles.TrialUser)
                .WithTrialEndDate(DateTimeOffset.MaxValue) //This is required to get past the default policy and onto the export specific policy
                .PostAsyncAssert("/api/data/excelexport", ExcelRequestData, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Test_MultiEntityExcelExportRequest_ReturnsForbidden_WhenTrialUser()
        {
            await InternalBrandVueApi
                .WithRole(Roles.TrialUser)
                .WithTrialEndDate(DateTimeOffset.MaxValue) //This is required to get past the default policy and onto the export specific policy
                .PostAsyncAssert("/api/data/excelexportmultientity", MultiEntityExcelRequestData, HttpStatusCode.Forbidden);
        }
    }
}
