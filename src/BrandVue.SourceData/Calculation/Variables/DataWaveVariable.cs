using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Calculation.Variables
{
    public class DataWaveVariable : IVariable<int?>
    {
        public Dictionary<int, DateRangeVariableComponent> WaveIdToWaveConditions { get; }
        private readonly long[] _orderedWaveEndTicks;
        private readonly (int EntityInstanceId, DateRangeVariableComponent Wave)[] _orderedWaves;
        private readonly bool _overlap;

        public DataWaveVariable(GroupedVariableDefinition varDef)
        {
            WaveIdToWaveConditions = new Dictionary<int, DateRangeVariableComponent>(varDef.Groups.Count);
            foreach (var group in varDef.Groups)
            {
                WaveIdToWaveConditions.Add(group.ToEntityInstanceId, (DateRangeVariableComponent)group.Component);
            }

            var dateRangeVariableComponents = varDef.Groups
                .Select(v => (v.ToEntityInstanceId, Wave: (DateRangeVariableComponent)v.Component))
                .OrderBy(w => w.Wave.MaxDate).ThenBy(w => w.Wave.MinDate)
                .ToArray();
            _orderedWaveEndTicks = dateRangeVariableComponents.Select(w => w.Wave.MaxDate.Ticks).ToArray();
            _orderedWaves = dateRangeVariableComponents;
            // Data wave variable doesn't depend on any fields/questions - we only care about survey's timestamp.
            FieldDependencies = Array.Empty<ResponseFieldDescriptor>();

            // This is the type containing entities which represent waves. Entity instances and entity types get created in BrandVueDataLoader.
            var waveEntityType = new EntityType(varDef.ToEntityTypeName, varDef.ToEntityTypeName, varDef.ToEntityTypeDisplayNamePlural);

            // Tell the engine that we need a separate variable function per each wave instance.
            UserEntityCombination = new[] { waveEntityType };

            var latestDate = DateTimeOffset.MinValue;
            foreach (var wave in _orderedWaves)
            {
                if (wave.Wave.MinDate <= latestDate)
                {
                    _overlap = true;
                    break;
                }
                latestDate = wave.Wave.MaxDate;
            }
        }

        public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
        {
            var requestedWave = entityValues.AsReadOnlyCollection().Single();
            var waveCondition = WaveIdToWaveConditions[requestedWave.Value];

            return profile =>
            {
                if (waveCondition.MinDate <= profile.Timestamp && profile.Timestamp <= waveCondition.MaxDate)
                    return requestedWave.Value;

                return null;
            };
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
        {
            var memoryPool = new ManagedMemoryPool<int>();
            return profile =>
            {
                memoryPool.FreeAll();
                int waveIndex = GetIndexOfFirstWaveWithMaxDateExceeding(profile.Timestamp.Ticks);

                var memory = memoryPool.Rent(_orderedWaveEndTicks.Length - waveIndex);
                int i = 0;
                for ( ;waveIndex < _orderedWaveEndTicks.Length; waveIndex++)
                {
                    var orderedWave = _orderedWaves[waveIndex];
                    if (orderedWave.Wave.MinDate <= profile.Timestamp)
                    {
                        if (valuePredicate(orderedWave.EntityInstanceId))
                        {
                            memory.Span[i++] = orderedWave.EntityInstanceId;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                return memory.Take(i);
            };
        }

        private int GetIndexOfFirstWaveWithMaxDateExceeding(long maxDateTicks)
        {
            var waveIndex = Array.BinarySearch(_orderedWaveEndTicks, maxDateTicks);
            if (waveIndex < 0) waveIndex = ~waveIndex;
            return waveIndex;
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            Enumerable.Empty<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>();

        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }

        public IReadOnlyCollection<EntityType> UserEntityCombination { get; }
        public bool OnlyDimensionIsEntityType() => true;
    }


    // This is a cut down version of the above variable just used for determining count/sample when creating a DataWaveVariable.
    // This is to prevent needing to create then remove entity types for the variable before the variable is actually created.
    // It returns 1 or null for a given respondent depending on if they are in the wave condition(s) for the variable.
    public class DataWaveProfileVariable : IVariable<int?>
    {
        public DateRangeVariableComponent[] WaveConditions { get; }

        public DataWaveProfileVariable(params VariableGrouping[] variableGroups)
        {
            WaveConditions = variableGroups.Select(g => (DateRangeVariableComponent)g.Component)
                .OrderBy(w => w.MaxDate).ThenBy(w => w.MinDate)
                .ToArray();

            FieldDependencies = Array.Empty<ResponseFieldDescriptor>();
            UserEntityCombination = Array.Empty<EntityType>();
        }

        public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
        {
            return profile =>
            {
                for (var i = 0; i < WaveConditions.Length; i++)
                {
                    var waveCondition = WaveConditions[i];
                    if (waveCondition.MinDate <= profile.Timestamp && profile.Timestamp <= waveCondition.MaxDate)
                        return 1;
                }

                return null;
            };
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            Enumerable.Empty<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>();

        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }

        public IReadOnlyCollection<EntityType> UserEntityCombination { get; }
        public bool OnlyDimensionIsEntityType() => false;
    }
}