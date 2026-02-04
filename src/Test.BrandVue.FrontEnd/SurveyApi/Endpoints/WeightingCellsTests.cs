using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class WeightingCellsTests
    {
        public class WeightingCellDescriptorComparer : IEqualityComparer<WeightingCellDescriptor>
        {

            public bool Equals(WeightingCellDescriptor x, WeightingCellDescriptor y)
            {
                if (x == y) return true;
                if (x is null) return false;
                if (y is null) return false;
                if (x.GetType() != y.GetType()) return false;
                
                return x.WeightingCellId == y.WeightingCellId &&
                       x.CellPartDescriptions.Keys.OrderBy(k=>k).SequenceEqual(y.CellPartDescriptions.Keys.OrderBy(kvp => kvp)) &&
                       x.CellPartDescriptions.Values.OrderBy(v=>v).SequenceEqual(y.CellPartDescriptions.Values.OrderBy(kvp => kvp)) &&
                         x.IsWeighted == y.IsWeighted;
            }

            public int GetHashCode(WeightingCellDescriptor obj)
            {
                return HashCode.Combine(obj.WeightingCellId, obj.CellPartDescriptions, obj.IsWeighted);
            }
        }
        [TestCase("/api/surveysets/UK/weightingcells")]
        public async Task GivenValidSurveysetThenResponseContainsApiAverageResults(string url)
        {
            await BrandVueTestServer.PublicSurveyApi.GetAsyncAssertOk(url, 
                ExpectedOutputs.WeightingCellDescriptors(), 
                new WeightingCellDescriptorComparer());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/weightingcells")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}