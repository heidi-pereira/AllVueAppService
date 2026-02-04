using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    [TestFixture]
    public class DateWaveVariableTests
    {
        private readonly DateTime _startOfFirstWave;
        private readonly TimeSpan _wavesDuration;
        private readonly DateTime _timeStampInFirstWave;
        private readonly DateTime _timeStampInSecondWave;
        private const string WaveEntityTypeName = "DataWave";

        public DateWaveVariableTests()
        {
            _startOfFirstWave = DateTime.Now;
            _wavesDuration = TimeSpan.FromDays(30);
            _timeStampInFirstWave = _startOfFirstWave.AddDays(1);
            _timeStampInSecondWave = _startOfFirstWave.Add(2 * _wavesDuration).AddDays(1);
        }

        [Test]
        public void GivenResponseTimeStampInFirstWave_AndFunctionSetUpForFirstWave_Function_ShouldReturnIdOfFirstWave()
        {
            var groupDefinition = GetDataWaveGroupDefinition(_startOfFirstWave, _wavesDuration);
            var variableFunction = GetDataWaveVariableFunction(groupDefinition, 1);

            var dataWaveId = variableFunction(new ProfileResponseEntity(1, _timeStampInFirstWave, -1));
            Assert.That(dataWaveId, Is.EqualTo(groupDefinition.Groups.First().ToEntityInstanceId));
        }

        [Test]
        public void GivenResponseTimeStampInSecondWave_AndFunctionSetUpForSecondWave_Function_ShouldReturnIdOfSecondWave()
        {
            var groupDefinition = GetDataWaveGroupDefinition(_startOfFirstWave, _wavesDuration);
            var variableFunction = GetDataWaveVariableFunction(groupDefinition, 2);

            var dataWaveId = variableFunction(new ProfileResponseEntity(1, _timeStampInSecondWave, -1));
            Assert.That(dataWaveId, Is.EqualTo(groupDefinition.Groups[1].ToEntityInstanceId));
        }

        [Test]
        public void GivenResponseTimeStampInFirstWave_AndFunctionSetUpForSecondWave_Function_ShouldReturnBlankAnswer()
        {
            var groupDefinition = GetDataWaveGroupDefinition(_startOfFirstWave, _wavesDuration);
            var variableFunction = GetDataWaveVariableFunction(groupDefinition, 2);

            var dataWaveId = variableFunction(new ProfileResponseEntity(1, _timeStampInFirstWave, -1));
            Assert.That(dataWaveId, Is.EqualTo(null));
        }

        [Test]
        public void GivenResponseTimeStampInSecondWave_AndFunctionSetUpForFirstWave_Function_ShouldReturnIdOfSecondWave()
        {
            var groupDefinition = GetDataWaveGroupDefinition(_startOfFirstWave, _wavesDuration);
            var variableFunction = GetDataWaveVariableFunction(groupDefinition, 2);

            var dataWaveId = variableFunction(new ProfileResponseEntity(1, _timeStampInFirstWave, -1));
            Assert.That(dataWaveId, Is.EqualTo(null));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void GivenResponseTimeStampOutsideOfAnyWaves_FunctionForAnyWave_ShouldReturnBlankAnswer(int waveId)
        {
            var groupDefinition = GetDataWaveGroupDefinition(_startOfFirstWave, _wavesDuration);
            var variableFunction = GetDataWaveVariableFunction(groupDefinition, waveId);

            var dataWaveId = variableFunction(new ProfileResponseEntity(1, DateTimeOffset.MinValue, -1));
            Assert.That(dataWaveId, Is.EqualTo(null));
        }

        internal static GroupedVariableDefinition GetDataWaveGroupDefinition(DateTime startOfFirstWave, TimeSpan waveDuration)
        {
            return new()
            {
                ToEntityTypeName = WaveEntityTypeName,
                ToEntityTypeDisplayNamePlural = "DataWaves",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceId = 1,
                        ToEntityInstanceName = "FirstWave",
                        Component = new DateRangeVariableComponent
                        {
                            MinDate = startOfFirstWave,
                            MaxDate = startOfFirstWave.Add(waveDuration)
                        },
                    },
                    new()
                    {
                        ToEntityInstanceId = 2,
                        ToEntityInstanceName = "SecondWave",
                        Component = new DateRangeVariableComponent
                        {
                            MinDate = startOfFirstWave.Add(2 * waveDuration),
                            MaxDate = startOfFirstWave.Add(3 * waveDuration)
                        },
                    }
                }
            };
        }

        private Func<IProfileResponseEntity, int?> GetDataWaveVariableFunction(GroupedVariableDefinition dataWaveVariableDefinition, int setUpFunctionForWaveInstanceId)
        {
            // The calculation engine will enumerate all possible entity values (waves) and create a variable function for each one. This is done in BaseMeasureTotalCalculator.
            var dataWaveVariable = new DataWaveVariable(dataWaveVariableDefinition);
            var getByEntityValue = dataWaveVariable.CreateForEntityValues(
                new EntityValueCombination(
                    new EntityValue(
                        new EntityType(WaveEntityTypeName, "wave", "waves"),
                        setUpFunctionForWaveInstanceId
                    )));
            var getAll = dataWaveVariable.CreateForSingleEntity(_ => true);
            return p =>
            {
                int? byEntityValue = getByEntityValue(p);
                int? singleValue = getAll(p).ToArray().Select<int, int?>(x => x).SingleOrDefault(x => x == setUpFunctionForWaveInstanceId);
                Assert.That(byEntityValue, Is.EqualTo(singleValue), "Sanity check that both ways of querying return the same value");
                return byEntityValue;
            };
        }
    }
}