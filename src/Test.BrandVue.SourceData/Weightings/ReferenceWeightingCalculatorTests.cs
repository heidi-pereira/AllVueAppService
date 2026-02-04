using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestCommon.Weighting;
using VerifyNUnit;

namespace Test.BrandVue.SourceData.Weightings
{
    [TestFixture]
    public class ReferenceWeightingCalculatorTests
    {

        [Test]
        public async Task SimpleRim()
        {
            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("Household composition_2", new[] { 1,2,3,4 })
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2,3 })

                .AddNumberOfResponsesForQuotaCell("1:0", 1950)
                .AddNumberOfResponsesForQuotaCell("2:0", 650)
                .AddNumberOfResponsesForQuotaCell("3:0", 455)
                .AddNumberOfResponsesForQuotaCell("4:0", 195)

                .AddNumberOfResponsesForQuotaCell("1:1", 1020)
                .AddNumberOfResponsesForQuotaCell("2:1", 340)
                .AddNumberOfResponsesForQuotaCell("3:1", 238)
                .AddNumberOfResponsesForQuotaCell("4:1", 102)

                .AddNumberOfResponsesForQuotaCell("1:2", 30)
                .AddNumberOfResponsesForQuotaCell("2:2", 10)
                .AddNumberOfResponsesForQuotaCell("3:2", 7)
                .AddNumberOfResponsesForQuotaCell("4:2", 3)

                .AddNumberOfResponsesForQuotaCell("1:3", 0)
                .AddNumberOfResponsesForQuotaCell("2:3", 0)
                .AddNumberOfResponsesForQuotaCell("3:3", 0)
                .AddNumberOfResponsesForQuotaCell("4:3", 0)
                ;


            var setup = testCase.SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task SimplePercentageWeightingPlanEqual()
        {

            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("0", 5000)
                .AddNumberOfResponsesForQuotaCell("1", 1000)
                .AddNumberOfResponsesForQuotaCell("2", 100)
                .AddNumberOfResponsesForQuotaCell("3", 200)
                ;


            var setup = testCase.SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimplePercentageWeightingPlan().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
            Assert.That(results.GetReferenceWeightingFor("1"), Is.EqualTo(results.GetReferenceWeightingFor("0")), "Gender 0 & 1 should have the same value");
        }

        [Test]
        public async Task SimplePercentageWeightingPlanNotEqual()
        {

            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("0", 1)
                .AddNumberOfResponsesForQuotaCell("1", 1000)
                .AddNumberOfResponsesForQuotaCell("2", 0)
                .AddNumberOfResponsesForQuotaCell("3", 0)
                ;


            var setup = testCase.SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimplePercentageWeightingPlanNonBalanced().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);
            Assert.That(results.GetReferenceWeightingFor("1"), Is.Not.EqualTo(results.GetReferenceWeightingFor("0")), "Gender 0 & 1 should NOT have the same value");

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

        }

        [Test]
        public async Task VerySimpleRim()
        {
            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("Gender", new[] {0,1,2 })
                .AddQuestionForQuotaAndInstances("Age", new[] { 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("0:1", 1237)
                .AddNumberOfResponsesForQuotaCell("0:2", 1238)
                .AddNumberOfResponsesForQuotaCell("0:3", 25)

                .AddNumberOfResponsesForQuotaCell("1:1", 743)
                .AddNumberOfResponsesForQuotaCell("1:2", 742)
                .AddNumberOfResponsesForQuotaCell("1:3", 15)

                .AddNumberOfResponsesForQuotaCell("2:1", 495)
                .AddNumberOfResponsesForQuotaCell("2:2", 495)
                .AddNumberOfResponsesForQuotaCell("2:3", 10)

                ;

            var setup = testCase.SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlan().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task VerySimpleRimZeroSample()
        {
            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2 })
                .AddQuestionForQuotaAndInstances("Age", new[] { 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("0:1", 0)
                .AddNumberOfResponsesForQuotaCell("0:2", 0)
                .AddNumberOfResponsesForQuotaCell("0:3", 0)

                .AddNumberOfResponsesForQuotaCell("1:1", 0)
                .AddNumberOfResponsesForQuotaCell("1:2", 0)
                .AddNumberOfResponsesForQuotaCell("1:3", 0)

                .AddNumberOfResponsesForQuotaCell("2:1", 0)
                .AddNumberOfResponsesForQuotaCell("2:2", 0)
                .AddNumberOfResponsesForQuotaCell("2:3", 0)

                ;
            var setup = testCase.SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlan().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        private ReferenceWeightingTestCase ReferenceTestCaseForSimpleWeightingPlanWithCountry()
        {
            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("COUNTRY", new[] { 100,200 })
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2 })
                .AddQuestionForQuotaAndInstances("Age", new[] { 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("100:0:1", 1237)
                .AddNumberOfResponsesForQuotaCell("100:0:2", 1238)
                .AddNumberOfResponsesForQuotaCell("100:0:3", 25)

                .AddNumberOfResponsesForQuotaCell("100:1:1", 743)
                .AddNumberOfResponsesForQuotaCell("100:1:2", 742)
                .AddNumberOfResponsesForQuotaCell("100:1:3", 15)

                .AddNumberOfResponsesForQuotaCell("100:2:1", 495)
                .AddNumberOfResponsesForQuotaCell("100:2:2", 495)
                .AddNumberOfResponsesForQuotaCell("100:2:3", 10)



                .AddNumberOfResponsesForQuotaCell("200:0:1", 1237)
                .AddNumberOfResponsesForQuotaCell("200:0:2", 1238)
                .AddNumberOfResponsesForQuotaCell("200:0:3", 25)

                .AddNumberOfResponsesForQuotaCell("200:1:1", 743)
                .AddNumberOfResponsesForQuotaCell("200:1:2", 742)
                .AddNumberOfResponsesForQuotaCell("200:1:3", 15)

                .AddNumberOfResponsesForQuotaCell("200:2:1", 495)
                .AddNumberOfResponsesForQuotaCell("200:2:2", 495)
                .AddNumberOfResponsesForQuotaCell("200:2:3", 10)

                ;
            return testCase;
        }

        [Test]
        public async Task VerySimpleRimWithWave()
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry().ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task TwoDeepRim()
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry();
            model.First().IsWeightingGroupRoot = false;

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

        }

        [TestCase(0.5, 0.5)]
        [TestCase(0.6, 0.4)]
        [TestCase(0.7, 0.3)]
        [TestCase(0.9, 0.1)]
        [TestCase(1.0, 0.0)]
        [TestCase(null, null)]
        public async Task TwoDeepRimWithNullGender(double? target1, double? target2)
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry(target1, target2);

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            Assert.That(results.TotalWeighting, Is.EqualTo(2.0).Within(0.000001) );
            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task TwoDeepRimWithExpansionFactor()
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountryTargetPopulation();

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            Assert.That(results.TotalWeighting, Is.EqualTo(2.0).Within(0.000001));
            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task TwoDeepRimWithNullGenderAndNoOther()
        {
            var testCase = new ReferenceWeightingTestCase(new List<QuotaCellQuestionAndInstances>(), new List<NumberOfResponsesForQuotaCell>());
            testCase
                .AddQuestionForQuotaAndInstances("COUNTRY", new[] { 100, 200 })
                .AddQuestionForQuotaAndInstances("Gender", new[] { 0, 1, 2 })
                .AddQuestionForQuotaAndInstances("Age", new[] { 1, 2, 3 })

                .AddNumberOfResponsesForQuotaCell("100:0:1", 1237)
                .AddNumberOfResponsesForQuotaCell("100:0:2", 1238)
                .AddNumberOfResponsesForQuotaCell("100:0:3", 25)

                .AddNumberOfResponsesForQuotaCell("100:1:1", 743)
                .AddNumberOfResponsesForQuotaCell("100:1:2", 742)
                .AddNumberOfResponsesForQuotaCell("100:1:3", 15)

                .AddNumberOfResponsesForQuotaCell("100:2:1", 0)
                .AddNumberOfResponsesForQuotaCell("100:2:2", 0)
                .AddNumberOfResponsesForQuotaCell("100:2:3", 0)


                .AddNumberOfResponsesForQuotaCell("200:0:1", 1237)
                .AddNumberOfResponsesForQuotaCell("200:0:2", 1238)
                .AddNumberOfResponsesForQuotaCell("200:0:3", 25)

                .AddNumberOfResponsesForQuotaCell("200:1:1", 743)
                .AddNumberOfResponsesForQuotaCell("200:1:2", 742)
                .AddNumberOfResponsesForQuotaCell("200:1:3", 15)

                .AddNumberOfResponsesForQuotaCell("200:2:1", 0)
                .AddNumberOfResponsesForQuotaCell("200:2:2", 0)
                .AddNumberOfResponsesForQuotaCell("200:2:3", 0)

                ;

            var setup = testCase.SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry();
            
            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }


        [Test]
        public async Task SimpleTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy().ToAppModel().ToArray();
            var setup = weightingPlansForDatabase.GenerateMatchingReferenceWeightingTestCase(totalRespondents: 5000).SetupProfileDistrubution();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            results.VerifyTargetWeightsModelMatch(weightingPlansForDatabase);
        }

        [Test]
        public async Task FilteredTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy().ToAppModel().ToArray();
            var setup = weightingPlansForDatabase.GenerateMatchingReferenceWeightingTestCase(totalRespondents: 5000).SetupProfileDistrubution();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

            results.VerifyTargetWeightsModelMatch(weightingPlansForDatabase);
        }

        [Test]
        public async Task WeightedFilteredTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.WeightedFilteredTargetOnlyStrategy().ToAppModel().ToArray();
            var setup = weightingPlansForDatabase.GenerateMatchingReferenceWeightingTestCase(totalRespondents: 5000).SetupProfileDistrubution();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

            results.VerifyTargetWeightsModelMatch(weightingPlansForDatabase,errorMargin:0.0001);
        }

        [Test]
        public async Task FilteredTargetOnlyStrategyWithNegative1()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategyWithNegative1().ToAppModel().ToArray();
            var setup = weightingPlansForDatabase.GenerateMatchingReferenceWeightingTestCase(totalRespondents: 5000).SetupProfileDistrubution();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

            results.VerifyTargetWeightsModelMatch(weightingPlansForDatabase);
        }

        [Test]
        public async Task WeightedFilteredTargetOnlyStrategyWithNegative1()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.WeightedFilteredTargetOnlyStrategyWithNegative1().ToAppModel().ToArray();
            var setup = weightingPlansForDatabase.GenerateMatchingReferenceWeightingTestCase(totalRespondents: 5000).SetupProfileDistrubution();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));

            results.VerifyTargetWeightsModelMatch(weightingPlansForDatabase, errorMargin: 0.0000001);
        }

        [Test]
        public async Task TwoDeepRimWithBalancedWeighting()
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry();

            var modelRoot = model.Single();
            var total = modelRoot.ChildTargets.Count();
            foreach(var child in modelRoot.ChildTargets)
            {
                child.Target = 1.0M / total;
            }

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }

        [Test]
        public async Task TwoDeepRimWithUnBalancedWeighting()
        {
            var setup = ReferenceTestCaseForSimpleWeightingPlanWithCountry().SetupProfileDistrubution();
            var model = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry();

            var modelRoot = model.Single();
            
            for(int index = 0; index < modelRoot.ChildTargets.Count; index++)
            {
                if (index == 0)
                {
                    modelRoot.ChildTargets[index].Target = 1.0M;
                }
                else
                {
                    modelRoot.ChildTargets[index].Target = 0.0M;
                }
            }

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var calc = new ReferenceWeightingCalculator(Substitute.For<ILogger>());
            var results = calc.CalculateReferenceWeightings(setup.accessor, setup.groupedQuotaCells, weightingPlansForDatabase);

            await Verifier.Verify(new QuotaCellReferenceWeightingsExposePrivates(results));
        }
    }
}
