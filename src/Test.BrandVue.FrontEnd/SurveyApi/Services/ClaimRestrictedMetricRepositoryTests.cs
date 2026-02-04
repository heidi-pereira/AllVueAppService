using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.Services;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    [TestFixture]
    public class ClaimRestrictedMetricRepositoryTests
    {
        [TestCase(new [] { "B_1&2" }, new [] { "Measure 1", "Measure 2" })]
        [TestCase(new [] { "A_1" }, new [] { "Measure 1" })]
        [TestCase(new [] { "A_1", "B_1&2" }, new [] { "Measure 1", "Measure 2" })]
        [TestCase(new [] { "A_1", "C_1&2", "Z_3" }, new [] { "Measure 1", "Measure 2", "Measure 3" })]
        [TestCase(new [] { "Y_3" }, new [] { "Measure 3" })]
        [TestCase(new [] { "M_0" }, new string[0] )]
        public void ClaimsRestrictedMetricRepositoryCorrectlyRemovesMetricsTheUserIsNotAllowedToSee(string[] allowedSubsets, string[] expectedMeasures)
        {
            var fakeMeasureRepo = Substitute.For<IMeasureRepository>();
            fakeMeasureRepo.GetAllMeasuresWithDisabledPropertyFalseForSubset(MockRepositoryData.UkSubset)
                .Returns(FakeMeasuresWithSubsetRestriction());

            var fakeClaimRestrictedSubsetRepo = Substitute.For<IClaimRestrictedSubsetRepository>();
            fakeClaimRestrictedSubsetRepo.GetAllowed()
                .Returns(allowedSubsets.Select(s => new Subset {Id = s}).ToList());

            var realClaimRestrictedMetricRepo = new ClaimRestrictedMetricRepository(fakeMeasureRepo, fakeClaimRestrictedSubsetRepo);
            var measures = realClaimRestrictedMetricRepo.GetAllowed(MockRepositoryData.UkSubset);

            Assert.That(measures.Count, Is.EqualTo(expectedMeasures.Length));
            Assert.That(measures.OrderBy(m => m.Name).Select(m => m.Name).SequenceEqual(expectedMeasures.OrderBy(e => e)), Is.True);
        }

        private static IEnumerable<Measure> FakeMeasuresWithSubsetRestriction()
        {
            var subsetRepository = new SubsetRepository();
            subsetRepository.Add(new Subset { Id = "A_1" });
            subsetRepository.Add(new Subset { Id = "B_1&2" });
            subsetRepository.Add(new Subset {Id = "C_1&2"});
            subsetRepository.Add(new Subset {Id = "D_2"});
            subsetRepository.Add(new Subset {Id = "X_3"});
            subsetRepository.Add(new Subset {Id = "Y_3"});
            subsetRepository.Add(new Subset {Id = "Z_3"});
            var measure =  new List<Measure>
            {
                new Measure
                {
                    Name = "Measure 1",
                },
                new Measure
                {
                    Name = "Measure 2",
                },
                new Measure
                {
                    Name = "Measure 3",
                },
            };
            measure[0].SetSubsets(new [] {"A_1", "B_1&2", "C_1&2"}, subsetRepository);
            measure[1].SetSubsets(new [] {"B_1&2", "C_1&2", "D_2" }, subsetRepository);
            measure[2].SetSubsets(new [] {"X_3", "Y_3", "Z_3"}, subsetRepository);
            return measure;
        }
    }
}
