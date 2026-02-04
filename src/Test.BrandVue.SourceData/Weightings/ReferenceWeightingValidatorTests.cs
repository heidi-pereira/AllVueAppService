using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Measures;
using NSubstitute;
using TestCommon.Weighting;
using BrandVue.SourceData.Calculation.Variables;
using Vue.Common.Auth;
using static BrandVue.SourceData.QuotaCells.ReferenceWeightingValidator;

namespace Test.BrandVue.SourceData.Weightings
{
    [TestFixture]
    public class ReferenceWeightingValidatorTests
    {
        [Test]
        public void SimpleRim()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan()
                .ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void VerySimpleRim()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlan()
                .ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void VerySimpleRimWithWave()
        {

            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects
                .GetVerySimpleRimWeightingPlanWithCountry().ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        [TestCase(0.5, 0.5)]
        [TestCase(0.6, 0.4)]
        [TestCase(0.7, 0.3)]
        [TestCase(0.9, 0.1)]
        [TestCase(1.0, 0.0)]
        [TestCase(1.0, null)]
        public void TwoDeepRimWithNullGender(double target1, double? target2)
        {
            var model =
                WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry(target1, target2);

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void TwoDeepRimWithExpansionFactor()
        {
            var model =
                WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountryTargetPopulation();

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }


        [Test]
        public void SimpleTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy()
                .ToAppModel().ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True, MessagesToText(messages));
        }

        string MessagesToText(IList<ReferenceWeightingValidator.Message> messages)
        {
            return string.Join(Environment.NewLine, messages.Select(x => $"{x.ErrorLevel} - {x.Path} {x.MessageText}"));
        }

        string MessagesToText(IList<ReferenceWeightingValidator.WeightingValidationMessage> messages)
        {
            return MessagesToText(ReferenceWeightingValidator.ConvertMessages(messages));
        }

        [Test]
        public void FilteredTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy()
                .ToAppModel().ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True, MessagesToText(messages));
        }

        [Test]
        public void WeightedFilteredTargetWeights()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.WeightedFilteredTargetOnlyStrategy()
                .ToAppModel().ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        static IEnumerable<IEnumerable<WeightingPlan>> WeightingPlans()
        {
            yield return WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy().ToAppModel();
            yield return WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy().ToAppModel();
            yield return WeightingPlanConfigurationsTestObjects.WeightedFilteredTargetOnlyStrategy().ToAppModel();
            yield return WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy().ToAppModel();
            yield return WeightingPlanConfigurationsTestObjects.WeightedFilteredTargetOnlyStrategyWithNegative1()
                .ToAppModel();
            yield return WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategyWithNegative1().ToAppModel();

        }

        [TestCaseSource(nameof(WeightingPlans))]
        public void TestIsNotPercentageWeighting(IEnumerable<WeightingPlan> plans)
        {
            foreach (var plan in plans)
            {
                Assert.That(plan.IsPercentageWeighting(), Is.False);
            }
        }

        [Test]
        public void SimplePercentageWeightingPlan()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimplePercentageWeightingPlan()
                .ToAppModel().ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True, MessagesToText(messages));
        }

        [Test]
        public void FilteredTargetOnlyStrategyWithNegative1()
        {
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects
                .FilteredTargetOnlyStrategyWithNegative1().ToAppModel().ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True, MessagesToText(messages));
        }

        [Test]
        public void TwoDeepRimWithBalancedWeighting()
        {

            var model = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry();

            var modelRoot = model.Single();
            var total = modelRoot.ChildTargets.Count();
            foreach (var child in modelRoot.ChildTargets)
            {
                child.Target = 1.0M / total;
            }

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void TwoDeepRimWithUnBalancedWeighting()
        {
            var model = WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountry();

            var modelRoot = model.Single();

            for (int index = 0; index < modelRoot.ChildTargets.Count; index++)
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

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, model.ToAppModel().ToArray(), new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);
        }


        // Cases that fail
        [TestCase(1.0, -1.0, TestName = "Nested Target has -1")]
        [TestCase(0.1, 0.1, TestName = "Nested Target does not add to 1")]
        public void TwoDeepRimWithNullGenderWithInvalidTargets(double? target1, double? target2)
        {

            var model =
                WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry(target1, target2);
            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(
                MessagesToText(messages),
                Is.EqualTo($"Error - Root Invalid nested target for Root <COUNTRY> 100={target1},200={target2}. Adds up to {target1 + target2}"));
            Assert.That(messages.First().InstanceIds.Count(), Is.EqualTo(2),
                $"{nameof(TwoDeepRimWithNullGenderWithInvalidTargets)} should have found 2 invalid targets");
            Assert.That(messages.First().InstanceIds.First(), Is.EqualTo(100),
                $"{nameof(TwoDeepRimWithNullGenderWithInvalidTargets)} first target instance id should be 100");
            Assert.That(messages.First().InstanceIds.Last(), Is.EqualTo(200),
                $"{nameof(TwoDeepRimWithNullGenderWithInvalidTargets)} last target instance id should be 200");
            Assert.That(messages.Any(x => x.ErrorLevel == ReferenceWeightingValidator.ErrorMessageLevel.Error), Is.True,
                "Failed to find an error");
            Assert.That(isValid, Is.False);

        }

        [Test]
        public void TestEmptyPlansWithNoResponseLevelWeights()
        {
            var model = new List<WeightingPlanConfiguration>();
            var weightingPlansForDatabase = model.ToAppModel().ToArray();


            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(MessagesToText(messages), Is.EqualTo("Error - Root Empty plans are not valid"));
            Assert.That(messages.Any(x => x.ErrorLevel == ReferenceWeightingValidator.ErrorMessageLevel.Error), Is.True,
                "Failed to find an error");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void TestAdhocSurveyWithResponseLevelWeights()
        {
            var model = new List<WeightingPlanConfiguration>();
            var weightingPlansForDatabase = model.ToAppModel().ToArray();


            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(true, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(isValid, Is.True);

        }

        [Test]
        public void TestTrackerSurveyWithResponseLevelWeights()
        {
            var weightingPlansForDatabase = WeightingPlanWithSingleVariableAndTwoInstancesOneResponseWeighted();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);
            Assert.That(
                MessagesToText(messages),
                Is.EqualTo("Warning - Root Question Root <Wave> not valid. Either add targets to (1) or add question with RIM or Response Level weightings below it."));

            Assert.That(isValid, Is.False);

        }

        [Test]
        public void TestTrackerSurveyWithoutResponseLevelWeights()
        {
            var weightingPlansForDatabase = WeightingPlanWithSingleVariableAndTwoInstances();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(
                MessagesToText(messages),
                Is.EqualTo("Warning - Root Question Root <Wave> not valid. Either add targets to (1,2) or add question with RIM or Response Level weightings below it."));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void WeightingPlanWithBothTargetPercentageAndPopulationShouldBeInvalid()
        {
            var plans = new WeightingPlan[]
            {
                new WeightingPlan("Wave",
                    new List<WeightingTarget>()
                    {
                        new WeightingTarget(null, 1, 0.5m, 1000, null, null),
                        new WeightingTarget(null, 2, 0.5m, 1000, null, null)
                    }, true, null, null),
            };
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, plans, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);
            Assert.That(isValid, Is.False);
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages.Select(m => m.ErrorType), Is.All.EqualTo(ErrorMessageType.MixedTargetPercentageAndPopulation));
        }

        [Test]
        public void NestedPlansWithTargetPopulationShouldBeInvalid()
        {
            var nestedPlans = new WeightingPlan[]
            {
                new WeightingPlan("Gender",
                    new List<WeightingTarget>()
                    {
                        new WeightingTarget(null, 1, null, 1000, null, null),
                        new WeightingTarget(null, 2, null, 1200, null, null)
                    }, false, null, null)
            };
            var plans = new WeightingPlan[]
            {
                new WeightingPlan("Wave",
                    new List<WeightingTarget>()
                    {
                        new WeightingTarget(nestedPlans, 1, null, null, null, null),
                        new WeightingTarget(nestedPlans, 2, null, null, null, null)
                    }, true, null, null),
            };
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, plans, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);
            Assert.That(isValid, Is.False);
            Assert.That(messages.Count, Is.EqualTo(2));
            Assert.That(messages.Select(m => m.ErrorType), Is.All.EqualTo(ErrorMessageType.TargetPopulationOutsideOfRoot));
        }

        private static IMeasureRepository MeasureRepository(params Measure[] measures)
        {
            var measureRepository = Substitute.For<IMeasureRepository>();
            measureRepository.TryGet(Arg.Any<string>(), out Arg.Any<Measure>()).Returns(args =>
            {
                var measureName = (string)args[0];
                var measure = measures.SingleOrDefault(m => m.Name == measureName);
                args[1] = measure;
                return measure != null;
            });

            return measureRepository;
        }

        private static DataWaveVariable WaveVariableWithPartialOverlappingWaves()
        {
            var groupedVariableDefinition = WeightingTestObjects.GetPartialOverlappingGroupedVariableDefinition();
            return new DataWaveVariable(groupedVariableDefinition);
        }

        private static DataWaveVariable WaveVariableWithDuplicateWaves()
        {
            var groupedVariableDefinition = WeightingTestObjects.GetDuplicateGroupedVariableDefinition();
            return new DataWaveVariable(groupedVariableDefinition);
        }

        private static readonly Measure WaveMeasureWithPartialOverlappingWaves = new()
        {
            Name = "Wave",
            VarCode = "Wave",
            PrimaryVariable = WaveVariableWithPartialOverlappingWaves(),
        };

        private static readonly Measure WaveMeasureWithDuplicateWaves = new()
        {
            Name = "Wave",
            VarCode = "Wave",
            PrimaryVariable = WaveVariableWithDuplicateWaves(),
        };

        static IEnumerable<Measure> OverlappingWaveMeasures()
        {
            yield return WaveMeasureWithPartialOverlappingWaves;
            yield return WaveMeasureWithDuplicateWaves;
        }

        [TestCaseSource(nameof(OverlappingWaveMeasures))]
        public void TestTrackerWithOverlappingWaves(Measure measure)
        {
            var measureRepository = MeasureRepository(measure);
            var weightingPlansForDatabase = WeightingPlanWithSingleVariableAndTwoInstancesOneResponseWeighted();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, measureRepository, out var messages);

            Assert.That(MessagesToText(messages),
                Is.EqualTo("Error - Root Overlapping waves for Root <Wave> 2,3"));
            Assert.That(isValid, Is.False);
        }

        private static WeightingPlan[] WeightingPlanWithSingleVariableAndTwoInstances()
        {
            var weightingPlansForDatabase = new WeightingPlan[]
            {
                new WeightingPlan("Wave",
                    new List<WeightingTarget>()
                    {
                        new WeightingTarget(null, 1, null, null, null, null),
                        new WeightingTarget(null, 2, null, null, null, null)
                    }, false, null, null),
            };
            return weightingPlansForDatabase;
        }

        private static WeightingPlan[] WeightingPlanWithSingleVariableAndTwoInstancesOneResponseWeighted()
        {
            var responseWeightingContext = new ResponseWeightingContext()
            {
                Id = 1,
                WeightingTargetId = 2,
                ResponseWeights = new List<ResponseWeightConfiguration>()
                {
                    new() { Id = 1, RespondentId = 1, Weight = 0.5m }, 
                    new() { Id = 2, RespondentId = 2, Weight = 0.5m }
                },
                Context = ""
            };

            var weightingPlansForDatabase = new WeightingPlan[]
            {
                new WeightingPlan("Wave",
                    new List<WeightingTarget>()
                    {
                        new WeightingTarget(null, 1, null, null, null, 1),
                        new WeightingTarget(null, 2, null, null, null, 2, responseWeightingContext)
                    }, false, null, null),
            };
            return weightingPlansForDatabase;
        }


        [Test]
        public void VariableUsedMoreThanOnceInTheTree()
        {
            var model = WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry();

            var variableToReUse = model.First().VariableIdentifier;

            model.First().ChildTargets.First().ChildPlans.First().VariableIdentifier = variableToReUse;

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(MessagesToText(messages),
                Is.EqualTo("Error - COUNTRY:100 Question COUNTRY:100 <COUNTRY> used more than once."));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void RimWeightingAcrossMultipleTreeDepths()
        {
            var model = WeightingPlanConfigurationsTestObjects.GetRimWeightingWithNullGendersPlanWithCountry();

            var firstInstanceOfWave = model.First().ChildTargets.First();

            var rimNode = firstInstanceOfWave.ChildPlans.First();
            var firstPartOfRim = rimNode.ChildTargets.First();

            var config = new WeightingPlanConfiguration
            {
                ParentTarget = firstPartOfRim,
                ProductShortCode = firstPartOfRim.ProductShortCode,
                SubProductId = firstPartOfRim.SubProductId,
                SubsetId = firstPartOfRim.SubsetId,
                VariableIdentifier = "TEST",
                ChildTargets = new List<WeightingTargetConfiguration>()
            };

            firstPartOfRim.ChildPlans = new List<WeightingPlanConfiguration>
            {
                config
            };

            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(MessagesToText(messages),
                Is.EqualTo("Error - COUNTRY:100 RIM (COUNTRY:100) contains sub-trees (<Gender> - Instances 0)"));
            Assert.That(messages.First().InstanceIds.Count(), Is.EqualTo(1),
                $"{nameof(RimWeightingAcrossMultipleTreeDepths)} should have found 1 instance id");
            Assert.That(messages.First().InstanceIds.First(), Is.EqualTo(0),
                $"{nameof(RimWeightingAcrossMultipleTreeDepths)} first target instance id should be 0");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void SingleTargetWeightingSchemeStrategyWithMissingTargets()
        {
            var model = WeightingPlanConfigurationsTestObjects.GetSingleTargetWeightingSchemeStrategyMissingTargets();

            IList<ReferenceWeightingValidator.WeightingValidationMessage> messages =
                new List<ReferenceWeightingValidator.WeightingValidationMessage>();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.ValidateVariablesExist(model.ToList(), new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), messages);

            Assert.That(MessagesToText(messages), Is.EqualTo("Error -  Variable Gender undefined"));
            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("DecemberWeeksWaveVariable", TestName = "No Targets test")]
        public void SimpleWeightingPlanWithNoTargets(string filterMetricName)
        {
            var newWeightingPlan = new WeightingPlan(filterMetricName, null, false, null, null);
            var weightingPlans = new List<WeightingPlan> { newWeightingPlan }.ToArray();
            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlans, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(MessagesToText(messages),
                Is.EqualTo($"Error - Root Question Root <{filterMetricName}> has no targets"));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void GroupedForWeightingTest()
        {
            var model = WeightingPlanConfigurationsTestObjects
                .GetWeightingPlanWithChildPlanMarkedAsWeightingGroupRoot();
            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(
                MessagesToText(messages),
                Is.EqualTo("Error - Gender:0 Question Gender:0 <Household composition_2> not valid. The parent 'Root' has been marked as grouped for weighting"));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void RIMInvalidLeafTargetsTest()
        {
            var model =
                WeightingPlanConfigurationsTestObjects.GetVerySimpleRimWeightingPlanWithCountryAndInvalidTargets();
            var weightingPlansForDatabase = model.ToAppModel().ToArray();

            var validator = new ReferenceWeightingValidator();
            var isValid = validator.IsValid(false, weightingPlansForDatabase, new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), out var messages);

            Assert.That(messages.Any(), Is.True, $"{nameof(RIMInvalidLeafTargetsTest)} failed to find an error");
            Assert.That(MessagesToText(messages), Is.EqualTo("Error - COUNTRY:100 Invalid nested target for COUNTRY:100 <Gender> 0=0.4,1=0.4,2=0.01. Adds up to 0.81"));
            Assert.That(isValid, Is.False);
        }
    }
}
