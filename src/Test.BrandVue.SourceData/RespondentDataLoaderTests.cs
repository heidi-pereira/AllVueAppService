using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.SourceData.Extensions;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class RespondentDataLoaderTests
    {
        private static readonly Subset Subset = TestResponseFactory.AllSubset;

        private static readonly EntityType Type1 = new EntityType(nameof(Type1), nameof(Type1), nameof(Type1));
        private static readonly EntityInstance Type1Instance1 = new EntityValue(Type1, 1).AsInstance();
        private static readonly EntityInstance Type1Instance4 = new EntityValue(Type1, 4).AsInstance();
        private static readonly EntityType Type2 = new EntityType(nameof(Type2), nameof(Type2), nameof(Type2));
        private static readonly EntityInstance Type2Instance6 = new EntityValue(Type2, 6).AsInstance();
        private static readonly EntityInstance Type2Instance7 = new EntityValue(Type2, 7).AsInstance();
        private static readonly EntityType Brand = TestEntityTypeRepository.Brand;
        private static readonly EntityInstance BrandInstance12 = new EntityValue(Brand, 12).AsInstance();
        private static readonly EntityInstance BrandInstance15 = new EntityValue(Brand, 15).AsInstance();
        private readonly EntityInstanceRepository _entityInstanceRepo;
        private readonly ResponseFieldManager _responseFieldManager;
        private readonly ResponseFieldDescriptor _profileField;
        private readonly ResponseFieldDescriptor _type1Field;
        private readonly ResponseFieldDescriptor _type2Field;
        private readonly ResponseFieldDescriptor _type1And2Field;
        private readonly ResponseFieldDescriptor _brandField;
        private readonly ResponseFieldDescriptor _separateBrandField;
        private readonly RespondentRepository _respondentRepository;

        public RespondentDataLoaderTests()
        {
            _entityInstanceRepo = new EntityInstanceRepository();
            _entityInstanceRepo.AddInstances(Brand.Identifier, BrandInstance12, BrandInstance15);
            _entityInstanceRepo.AddInstances(Type1.Identifier, Type1Instance1, Type1Instance4);
            _entityInstanceRepo.AddInstances(Type2.Identifier, Type2Instance6, Type2Instance7);
            _responseFieldManager = new ResponseFieldManager(new TestEntityTypeRepository());
            _profileField = _responseFieldManager.Add(nameof(_profileField));
            _type1Field = _responseFieldManager.Add(nameof(_type1Field), Type1);
            _type2Field = _responseFieldManager.Add(nameof(_type2Field), Type2);
            _type1And2Field = _responseFieldManager.Add(nameof(_type1And2Field), Type1, Type2);
            _brandField = _responseFieldManager.Add(nameof(_brandField), Brand);
            _separateBrandField = _responseFieldManager.Add(nameof(_separateBrandField), Brand);
            _respondentRepository = new RespondentRepository(Subset);
        }

        [Test]
        public async Task GivenEmptyRepo_WhenRequestedProfileFieldWithBrandBaseField_ThenLoadsProfileOnceAndThenPerBrand()
        {
            ResponseFieldDescriptor field = _profileField, baseField = _brandField;

            var measure = CreateMeasureReferencing(field, baseField);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var instances = new []{new TargetInstances(Brand, new[]{BrandInstance12})};
            var loader = CreateRealLoader(lazyDataLoader, _entityInstanceRepo);
            await RealPossiblyLoadMeasures(loader, measure, instances);

            int callsForFirstLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForFirstLoad, Is.EqualTo(2), "Should have one for profile, then one for brand");

            var adjacentInstances = new []{new TargetInstances(Brand, new[]{BrandInstance15})};
            await RealPossiblyLoadMeasures(loader, measure, adjacentInstances );
            int callsForAdjacentLoadOfBrand = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForAdjacentLoadOfBrand, Is.EqualTo(3), "Profile field already got, so should have one extra for the new brand requested (since brands are currently excluded from the adjacent entity heuristic)");
        }

        [Test, Timeout(2000)]
        public async Task GivenQueuedThread_WhenThreadIsCancelled_ThenExceptionIsThrownAsync()
        {
            ResponseFieldDescriptor field = _profileField, baseField = _brandField;

            var measure = CreateMeasureReferencing(field, baseField);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();
            var dbDataTaskCancellation = new CancellationTokenSource();
            var queueingTaskCancellation = new CancellationTokenSource();
            var dbDataDummyTask = new TaskCompletionSource<EntityMetricData[]>();
            dbDataDummyTask.SetCanceled(dbDataTaskCancellation.Token);

            lazyDataLoader.GetDataForFields(null, null, null, null, default).ReturnsForAnyArgs(dbDataDummyTask.Task);
            var instances = new []{new TargetInstances(Brand, new[]{BrandInstance12})};
            var loader = CreateRealLoader(lazyDataLoader, _entityInstanceRepo);

            var neverCancelInTestCode = CancellationToken.None; // Don't pass to Task.Run, we want to test the production cancellation token usage
            var taskWaitingForDbData = Task.Run(async () => await RealPossiblyLoadMeasures(loader, measure, dbDataTaskCancellation.Token, instances), neverCancelInTestCode);
            var taskWaitingInQueue = Task.Run(async () => await RealPossiblyLoadMeasures(loader, measure, queueingTaskCancellation.Token, instances), neverCancelInTestCode);

            // Cancel queued thread
            queueingTaskCancellation.Cancel();
            Assert.That(async () => await taskWaitingInQueue, Throws.InstanceOf<OperationCanceledException>());

            // Cancel thread waiting on data from db
            dbDataTaskCancellation.Cancel();
            Assert.That(async () => await taskWaitingForDbData, Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task GivenEmptyRepo_WhenRequestedTwoMeasuresWithSameBaseFieldAndEntity_ThenLoadsBaseOnlyOnce()
        {
            var measure1 = CreateMeasureReferencing(_type1Field, _brandField);
            var measure2 = CreateMeasureReferencing(_type2Field, _brandField);
            var sameBrandInstanceToRequest = new TargetInstances(Brand, new[]{BrandInstance12});
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();
            var measure1Instances = new []{sameBrandInstanceToRequest, new TargetInstances(Type1, new[]{Type1Instance1})};
            var loader = CreateRealLoader(lazyDataLoader, _entityInstanceRepo);

            await RealPossiblyLoadMeasures(loader, measure1, measure1Instances);
            int callsForFirstLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForFirstLoad, Is.EqualTo(2), $"Should have one for {nameof(_type1Field)}, then one for {nameof(_brandField)}");

            var measure2Instances = new [] {sameBrandInstanceToRequest, new TargetInstances(Type2, new[]{Type2Instance6})};
            await RealPossiblyLoadMeasures(loader, measure2, measure2Instances);
            int callsForLoadWithSameBaseFieldAndEntity = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForLoadWithSameBaseFieldAndEntity, Is.EqualTo(3), $"{nameof(_brandField)} for entity already loaded, so should have one extra for the new type2Field requested");
        }

        [Test] // These "Range" attributes mean we'll generate all combinations
        public async Task GivenEmptyRepo_WhenRequestedTwice_ThenLoadsOnce([Range(0, 3)] int fieldIndex, [Range(0, 3)] int baseFieldIndex)
        {
            var fieldsToTest= new[] {_profileField, _type1Field, _type2Field, _type1And2Field};
            ResponseFieldDescriptor field = fieldsToTest[fieldIndex], baseField = fieldsToTest[baseFieldIndex];
            var measure = CreateMeasureReferencing(field, baseField);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var instances = GetInstancesAt(0, field, baseField);
            var loader = CreateRealLoader(lazyDataLoader, _entityInstanceRepo);
            await RealPossiblyLoadMeasures(loader, measure, instances);

            int callsForFirstLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForFirstLoad, Is.GreaterThanOrEqualTo(1), "Must load some data");
            Assert.That(callsForFirstLoad, Is.LessThanOrEqualTo(2), "No more than one request per field");

            await RealPossiblyLoadMeasures(loader, measure, instances);
            int callsForSecondIdenticalLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForSecondIdenticalLoad, Is.EqualTo(callsForFirstLoad), "Should already be loaded, so no more calls");
        }

        [Test] // These "Range" attributes mean we'll generate all combinations
        public async Task GivenEmptyRepo_WhenRequestedAdjacent_ThenLoadsOnce([Range(0, 3)] int fieldIndex, [Range(0, 3)] int baseFieldIndex)
        {
            var fieldsToTest= new[] {_profileField, _type1Field, _type2Field, _type1And2Field};
            ResponseFieldDescriptor field = fieldsToTest[fieldIndex], baseField = fieldsToTest[baseFieldIndex];
            var measure = CreateMeasureReferencing(field, baseField);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();


            var instances = GetInstancesAt(0, field, baseField);
            var loader = CreateRealLoader(lazyDataLoader, _entityInstanceRepo);
            await RealPossiblyLoadMeasures(loader, measure, instances);

            int callsForFirstLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForFirstLoad, Is.GreaterThanOrEqualTo(1), "Must load some data");
            Assert.That(callsForFirstLoad, Is.LessThanOrEqualTo(2), "No more than one request per field");

            var adjacentInstances = GetInstancesAt(1, field, baseField);
            await RealPossiblyLoadMeasures(loader, measure, adjacentInstances);
            int callsForAdjacentLoad = lazyDataLoader.ReceivedCalls().Count();
            Assert.That(callsForAdjacentLoad, Is.EqualTo(callsForFirstLoad), "For performance reasons, we pull some adjacent entities in the first request");
        }

        [Test]
        public void SeparateFieldsWithSameEntityTypeShouldNotCauseDuplicateDataTargets()
        {
            var measure = CreateMeasureReferencing(_brandField, _separateBrandField);

            var variableTargetInstances = new TargetInstances(Type1, _entityInstanceRepo.GetInstancesOf(Type1.Identifier, Subset));
            var brandInstances = _entityInstanceRepo.GetInstancesOf(Brand.Identifier, Subset);
            var brandTargetInstancesA = new TargetInstances(Brand, brandInstances.Take(1));
            var brandTargetInstancesB = new TargetInstances(Brand, brandInstances.Skip(1));
            var targetInstances = new[] { variableTargetInstances, brandTargetInstancesA, brandTargetInstancesB };

            //mimicking things that happen in DataPresenceGuarantor.EnsureDataIsLoaded and RespondentMeasureDataLoader.PossiblyLoadMeasures
            var fieldsForTargetInstances = measure.GetFieldDependencies();
            var groups = EntityCombinationFieldGroup.CreateGroups(Subset, fieldsForTargetInstances);
            Assert.That(groups.Count, Is.EqualTo(1), "Fields with the same entity column should be grouped together");
            var group = groups.Single();
            var groupTargetInstances = group.GetRelevantTargetInstances(targetInstances);
            Assert.That(groupTargetInstances.Count, Is.EqualTo(1), "Duplicate data targets should be merged");
            var instanceIdsToBeLoaded = groupTargetInstances.Single().SortedEntityInstanceIds;
            Assert.That(instanceIdsToBeLoaded, Is.EquivalentTo(brandInstances.Select(i => i.Id)), "Merged data targets should contain all IDs that were in the separate parts");
        }

        private TargetInstances[] GetInstancesAt(int index, params ResponseFieldDescriptor[] fields)
        {
            return fields.SelectMany(f => f.EntityCombination).Distinct()
                .Select(e => (Type: e, Instance: _entityInstanceRepo.GetInstancesOf(e.Identifier, Subset).ElementAtOrDefault(index)))
                .Where(x => x.Instance != null)
                .Select(i => new TargetInstances(i.Type, new[] {i.Instance}))
                .ToArray();
        }

        private async Task RealPossiblyLoadMeasures(IRespondentDataLoader loader, Measure measure,
            params TargetInstances[] targetInstances)
        {
            await RealPossiblyLoadMeasures(loader, measure, cancellationToken: default, targetInstances);
        }

        private async Task RealPossiblyLoadMeasures(IRespondentDataLoader loader, Measure measure, CancellationToken cancellationToken,
            params TargetInstances[] targetInstances)
        {
            await loader.PossiblyLoadMeasures(_respondentRepository, Subset, new (measure.GetFieldDependencies().ToArray(), targetInstances),
                DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks, cancellationToken);
        }

        private static RespondentDataLoader CreateRealLoader(ILazyDataLoader lazyDataLoader, EntityInstanceRepository entityInstanceRepo)
        {
            return new RespondentDataLoader(lazyDataLoader, entityInstanceRepo, new ConfigurationSourcedLoaderSettings(new AppSettings()));
        }

        private static Measure CreateMeasureReferencing(ResponseFieldDescriptor field, ResponseFieldDescriptor baseField)
        {
            var measure = new Measure
            {
                Name = "Details don't matter other than referencing two the fields",
                CalculationType = CalculationType.YesNo,
                Field = field,
                LegacyPrimaryTrueValues = { Values = new[] {1} },
                BaseField = baseField,
                LegacyBaseValues = { Values = new[] {3, 4, 5} },
            };
            return measure;
        }
    }
}
