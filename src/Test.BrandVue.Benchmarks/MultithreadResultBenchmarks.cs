using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class MultithreadResultBenchmarks
    {
        private readonly ResponseFieldManager _responseFieldManager;
        private readonly RespondentRepository _respondentRepository;
        private readonly RespondentDataLoaderBenchmark _respondentDataLoader;
        private readonly ProductionCalculatorBuilder _calculatorBuilder;
        private BenchmarkThreadHelper _threadHelper;
        private const int NumberOfIterations = 1000;
        private const int NumberOfFieldsPerThread = 8;
        private static readonly EntityType SurveyTakenAnswer = new EntityType("surveytaken", "Answer", "Answers");
        private static readonly EntityValue ChosenBrand = new EntityValue(TestEntityTypeRepository.Brand, 1);
        private static readonly EntityValue OtherBrand = new EntityValue(TestEntityTypeRepository.Brand, 2);
        private static readonly EntityValue ChosenProduct = new EntityValue(TestEntityTypeRepository.Product, 11);
        private static readonly EntityValue OtherProduct = new EntityValue(TestEntityTypeRepository.Product, 12);
        private static readonly EntityValue TakenSurvey = new EntityValue(SurveyTakenAnswer, 21);

        public MultithreadResultBenchmarks()
        {
            var entityRepository = new TestEntityInstanceRepository(ChosenBrand, OtherBrand, ChosenProduct, OtherProduct, TakenSurvey);
            var testResponseEntityTypeRepository = new TestEntityTypeRepository(SurveyTakenAnswer);
            var lazyDataLoader = Substitute.For<ILazyDataLoader>();
            _respondentDataLoader = new RespondentDataLoaderBenchmark(lazyDataLoader, entityRepository, new ConfigurationSourcedLoaderSettings(new AppSettings()));
            _responseFieldManager = new ResponseFieldManager(NullLogger<ResponseFieldManager>.Instance, testResponseEntityTypeRepository);
            _calculatorBuilder = new ProductionCalculatorBuilder(includeResponseIds: false).WithAverage(Averages.SingleDayAverage).WithFilterInstance(ChosenBrand);
            _respondentRepository = new RespondentRepository(_calculatorBuilder.Subset);
        }
        
        
        /// <summary>
        /// Here many threads are intialising and reading from the same profile, creating a lot of contention.
        /// If the ProfileResponseEntity writes are not threadsafe this will throw
        /// We suspect a thread safety issue in the reads 
        /// </summary>
        [GlobalSetup(Target = nameof(WriteReadLockingBenchmark))]
        public void SetupWriteReadLocked()
        {
            _threadHelper = new BenchmarkThreadHelper(16);
            
            var qc = new QuotaCell(1, _calculatorBuilder.Subset, new Dictionary<string, int>());
            var pr = new ProfileResponseEntity(1, new DateTime(2012, 1, 1), 1);
            _respondentRepository.Add(pr, qc);
            
            for (int i = 0; i < NumberOfIterations; i++)
            {
                _responseFieldManager.Add($"Generic{i}", TestEntityTypeRepository.GenericQuestion );
            }
            var actions = Enumerable.Range(0,NumberOfIterations).Select(i => PopulateAndReadSampleResponses(i));
            foreach (var action in actions)
            {
                _threadHelper.Add(action);
            }
        }

        /// <summary>
        /// Each thread will read every profile in the same order to maximise read contention.
        /// </summary>
        [GlobalSetup(Target = nameof(ReadReadLockingBenchmark)), SetUp]
        public void SetupReadReadLocked()
        {
            _threadHelper = new BenchmarkThreadHelper(16);
            for (int i = 0; i < NumberOfFieldsPerThread / 2; i++)
            {
                _responseFieldManager.Add($"Generic{i}", TestEntityTypeRepository.GenericQuestion );
            }
            var qc = new QuotaCell(1, _calculatorBuilder.Subset, new Dictionary<string, int>());
            var pr = new ProfileResponseEntity(1, new DateTime(2012, 1, 1), 1);
            _respondentRepository.Add(pr, qc);
            var emdArray = CreateEmdArray(0, out var answers, NumberOfFieldsPerThread / 2);
            _respondentDataLoader.PopulateResponsesFromData(_respondentRepository, emdArray, _calculatorBuilder.Subset);
            
            var actions = Enumerable.Range(0,NumberOfIterations).Select(i => ReadSampleResponses(i, answers));
            foreach (var action in actions)
            {
                _threadHelper.Add(action);
            }
        }

        [Benchmark]
        public void WriteReadLockingBenchmark()
        {
            _threadHelper.ExecuteAndWait();
        }
        
        // [Benchmark]
        // public void MultipleProfileThreadingBenchmark()
        // {
        //     _threadHelper.ExecuteAndWait();
        // }
        
        [Benchmark, Test]
        public void ReadReadLockingBenchmark()
        {
            _threadHelper.ExecuteAndWait();
        }
        
        private Action PopulateAndReadSampleResponses(int iteration)
        {
            var emdArray = CreateEmdArray(iteration, out var answers, NumberOfFieldsPerThread);
            var memoryPool = new ManagedMemoryPool<int>();
            return () =>
            {
                memoryPool.FreeAll();
                _respondentDataLoader.PopulateResponsesFromData(_respondentRepository, emdArray, _calculatorBuilder.Subset);
                foreach (var answer in answers)
                {
                    var value = _respondentRepository.Get(1).ProfileResponseEntity
                        .GetIntegerFieldValues(answer.Field, (_) => true, x => x.Key[0], memoryPool);
                    Assert.That(value.ToArray().Single(), Is.EqualTo(answer.FieldValue));
                }
            };
        }

        private Action ReadSampleResponses(int iteration, List<TestAnswer> answers)
        {
            var memoryPool = new ManagedMemoryPool<int>();
            return () =>
            {
                memoryPool.FreeAll();
                foreach (var answer in answers)
                {
                    var value = _respondentRepository.Get(1).ProfileResponseEntity
                        .GetIntegerFieldValues(answer.Field, (_) => true, x => x.Key[0], memoryPool);
                    Assert.That(value.ToArray().Single(), Is.EqualTo(answer.FieldValue));
                }
            };
        }

        private EntityMetricData[] CreateEmdArray(int iteration, out List<TestAnswer> answers, int numberOfFields)
        {
            var emds = new List<EntityMetricData>();
            answers = new List<TestAnswer>();
            for (int i = 0; i < numberOfFields; i++)
            {
                var fieldId = (iteration + i) % NumberOfIterations;
                var answer = TestAnswer.For(_responseFieldManager.Get($"Generic{fieldId}"), fieldId,
                    new EntityValue(TestEntityTypeRepository.GenericQuestion, fieldId));
                answers.Add(answer);
                emds.Add(new EntityMetricData
                {
                    ResponseId = 1,
                    EntityIds = answer.EntityValues.EntityIds,
                    Measures = (answer.Field, answer.FieldValue).Yield().ToList(),
                    Timestamp = new DateTimeOffset(new DateTime(2012, 01, 01)),
                    SurveyId = 0
                });
            }

            var emdArray = emds.ToArray();
            return emdArray;
        }
    }
    
    class RespondentDataLoaderBenchmark : RespondentDataLoader
    {
        public RespondentDataLoaderBenchmark(ILazyDataLoader lazyDataLoader, IEntityRepository entityRepository, IBrandVueDataLoaderSettings settings) : base(lazyDataLoader, entityRepository, settings)
        {
        }
        
        public new void PopulateResponsesFromData(IRespondentRepository respondentRepository,
            EntityMetricData[] entityMeasureData, Subset subset)
        {
            foreach (var entityMeasure in entityMeasureData)
            {
                foreach (var measureFieldValue in entityMeasure.Measures)
                {
                    measureFieldValue.Field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
                }
            }

            base.PopulateResponsesFromData(respondentRepository, entityMeasureData, subset);
        }
    }
}
