using BrandVue.Services;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public static class SampleSizeMetadataExtensions
    {
        public static SampleSizeMetadata GetSampleSizeMetadata(this WeightedDailyResult result)
        {
            return new SampleSizeMetadata
            {
                SampleSize = new UnweightedAndWeightedSample
                {
                    Unweighted = result?.UnweightedSampleSize ?? 0,
                    Weighted = result?.WeightedSampleSize ?? 0
                },
                CurrentDate = result?.Date
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this WeightedDailyResult[] results)
        {
            var lastDataPoint = results?.LastOrDefault();

            return new SampleSizeMetadata
            {
                SampleSize = new UnweightedAndWeightedSample
                {
                    Unweighted = lastDataPoint?.UnweightedSampleSize ?? 0,
                    Weighted = lastDataPoint?.WeightedSampleSize ?? 0
                },
                CurrentDate = lastDataPoint?.Date
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this WeightedDailyResult[][] results)
        {
            if (!results.Any())
            {
                return new SampleSizeMetadata();
            }

            var outerResults = results.Select(r => r.Where(d => d != null).ToArray().GetSampleSizeMetadata()).ToArray();
            var sampleSize = SumSampleSizes(outerResults.Select(r => r.SampleSize));

            return new SampleSizeMetadata
            {
                SampleSize = sampleSize,
                CurrentDate = outerResults[0].CurrentDate
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this EntityWeightedDailyResults[] results)
        {
            return new SampleSizeMetadata
            {
                SampleSize = SumSampleSizes(results.Select(r => r.WeightedDailyResults.LastOrDefault())),
                SampleSizeByEntity = results.ToDictionary(e => e.EntityInstance?.Name ?? "", m => new UnweightedAndWeightedSample
                {
                    Unweighted = m.WeightedDailyResults.LastOrDefault()?.UnweightedSampleSize ?? 0.0,
                    Weighted = m.WeightedDailyResults.LastOrDefault()?.WeightedSampleSize ?? 0.0
                }),
                CurrentDate = results.FirstOrDefault()?.WeightedDailyResults?.LastOrDefault()?.Date
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this ScorecardPerformanceMetricResult[] results)
        {
            var metaForEachMetric = results.Select(r => new { r.MetricName, SampleSizeMeta = r.PeriodResults.ToArray().GetSampleSizeMetadata() }).ToArray();

            if (!metaForEachMetric.Any())
            {
                return new SampleSizeMetadata();
            }

            return new SampleSizeMetadata
            {
                SampleSize = metaForEachMetric.Max(r => r.SampleSizeMeta.SampleSize),
                SampleSizeByMetric = metaForEachMetric.ToDictionary(m => m.MetricName, m => m.SampleSizeMeta.SampleSize),
                CurrentDate = metaForEachMetric[0].SampleSizeMeta.CurrentDate
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this ScorecardVsKeyCompetitorsMetricResults[] results)
        {
            if (!results.Any())
            {
                return new SampleSizeMetadata();
            }

            var maxResult = results.MaxBy(r => r.ActiveEntityResult.Current?.UnweightedSampleSize ?? 0);

            return new SampleSizeMetadata
            {
                SampleSize = new UnweightedAndWeightedSample
                {
                    Unweighted = maxResult?.ActiveEntityResult.Current?.UnweightedSampleSize ?? 0,
                    Weighted = maxResult?.ActiveEntityResult.Current?.WeightedSampleSize ?? 0
                },
                SampleSizeByMetric = results.ToDictionary(r => r.MetricName, r => new UnweightedAndWeightedSample
                {
                    Unweighted = r.ActiveEntityResult.Current?.UnweightedSampleSize ?? 0.0,
                    Weighted = r.ActiveEntityResult.Current?.WeightedSampleSize ?? 0.0
                }),
                CurrentDate = results[0].ActiveEntityResult.Current?.Date
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this BrokenDownResults[] results)
        {
            var sampleSizeMetaPerEntityInstance = results.Select(r => r.Total.ToArray().GetSampleSizeMetadata()).ToArray();
            var sampleSize = sampleSizeMetaPerEntityInstance.Max(s => s.SampleSize);

            return new SampleSizeMetadata
            {
                SampleSize = sampleSize,
                CurrentDate = sampleSizeMetaPerEntityInstance[0].CurrentDate
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this InstanceResult[] results)
        {
            (EntityInstance Instance, CellResult Result) GetOverallResult(InstanceResult result)
            {
                var overallResult = result.Values.SingleOrDefault(kvp => kvp.Key == CrosstabResultsProvider.TotalScoreColumn);
                return (result.EntityInstance, overallResult.Value);
            }

            var sampleSizeByEntity = results.Select(r => GetOverallResult(r))
                .ToDictionary(x => x.Instance.Name, x => x.Result?.SampleSizeMetaData?.SampleSize ?? default);
            return new SampleSizeMetadata
            {
                SampleSize = SumSampleSizes(sampleSizeByEntity.Values),
                SampleSizeByEntity = sampleSizeByEntity,
                CurrentDate = results.First().Values.First().Value.SampleSizeMetaData.CurrentDate
            };
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this BreakResults[] results, int? sampleSizeEntityInstanceId = null)
        {
            var focusEntityInstance = sampleSizeEntityInstanceId.HasValue ?
                results.FirstOrDefault()?.EntityResults.FirstOrDefault(r => r.EntityInstance?.Id == sampleSizeEntityInstanceId.Value)?.EntityInstance :
                null;
            var samplePerBreak = results
                .Select(r =>
                {
                    var entityResults = r.EntityResults;
                    if (sampleSizeEntityInstanceId.HasValue)
                    {
                        entityResults = entityResults.Where(e => e.EntityInstance?.Id == sampleSizeEntityInstanceId.Value).ToArray();
                    }
                    return (EntityInstanceName: r.BreakName, SampleSizeMetadata: entityResults.GetSampleSizeMetadata());
                })
                .ToArray();
            return GetMultipleDimensionSampleSizeMetadata(samplePerBreak, focusEntityInstance);
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this StackedInstanceResult[] results)
        {
            var samplePerFilterInstance = results
                .Select(r => (EntityInstanceName: r.FilterInstance.Name, SampleSizeMeta: r.Data.GetSampleSizeMetadata()))
                .ToArray();
            return GetMultipleDimensionSampleSizeMetadata(samplePerFilterInstance);
        }

        public static SampleSizeMetadata GetSampleSizeMetadata(this ResultsPerWave[] results)
        {
            //If sample is the same across a wave, sample will be shown per wave
            //If sample is the same for a break within a wave, sample will be shown per wave + break
            //Otherwise, no sample will be shown

            SampleSizeMetadata GetSampleSizeMetaData(IGrouping<string, (string WaveName, string BreakName, SampleSizeMetadata SampleSizeMeta)>[] groupedSamples)
            {
                return new SampleSizeMetadata
                {
                    SampleSize = SumSampleSizes(groupedSamples.Select(waveGroup => waveGroup.First().SampleSizeMeta.SampleSize)),
                    SampleSizeByEntity = groupedSamples.ToDictionary(
                        waveGroup => waveGroup.Key,
                        waveGroup => waveGroup.First().SampleSizeMeta.SampleSizeByEntity.First().Value),
                    CurrentDate = groupedSamples.First().First().SampleSizeMeta.CurrentDate
                };
            }

            var samples = results
                .SelectMany(r => r.WaveResults.Select(result => (WaveName: result.WaveName, BreakName: r.BreakName, SampleSizeMeta: result.EntityResults.GetSampleSizeMetadata())));
            var samplePerWave = samples.GroupBy(r => r.WaveName)
                .ToArray();

            if (samplePerWave.All(waveGroup => HasSameEntitySampleSizes(waveGroup.Select(g => g.SampleSizeMeta))))
            {
                return GetSampleSizeMetaData(samplePerWave);
            }

            if (samplePerWave.All(waveGroup => waveGroup.First().BreakName != null))
            {
                var samplePerWaveAndBreak = samples.GroupBy(r => $"{r.WaveName}: {r.BreakName}")
                    .ToArray();
                return GetSampleSizeMetaData(samplePerWaveAndBreak);
            }

            return null;
        }

        public static SampleSizeMetadata GetSampleSizeMetadata (this ResultsProviderParameters splitMetricPam,
            (string Label, WeightedDailyResult Result)[][] results, WeightedDailyResult[][] orderedResults, IEntityRepository entityRepository)
        {
            SampleSizeMetadata sampleSizeMetadata;
            if (splitMetricPam.FocusEntityInstanceId.HasValue)
            {
                if (!splitMetricPam.RequestedInstances.OrderedInstances.Any(x =>
                        x.Id == splitMetricPam.FocusEntityInstanceId))
                {
                    throw new ArgumentException(
                        "Focus Instance must be one of and the same type as the requested instances");
                }

                EntityInstance focusEntity =
                    entityRepository.GetInstances(splitMetricPam.RequestedInstances.EntityType.Identifier,
                            new[] { splitMetricPam.FocusEntityInstanceId.Value }, splitMetricPam.Subset)
                        .First();
                string entityInstanceName = focusEntity.Name;

                var sampleSizeDailyResults = results
                    .SelectMany(result => result.Where(r => r.Label == entityInstanceName).Select(r => r.Result)).ToList();

                sampleSizeMetadata = sampleSizeDailyResults.Any()
                    ? new SampleSizeMetadata { SampleSize = SumSampleSizes(sampleSizeDailyResults) }
                    : orderedResults.GetSampleSizeMetadata();
            }
            else
            {
                sampleSizeMetadata = orderedResults.GetSampleSizeMetadata();
            }

            return sampleSizeMetadata;
        }

        private static SampleSizeMetadata GetMultipleDimensionSampleSizeMetadata((string EntityInstanceName, SampleSizeMetadata SampleSizeMeta)[] samplePerInstance, EntityInstance sampleSizeEntityInstance = null)
        {
            //This is trying to handle multiple cases so shows different entities depending which one it finds
            //Case A: all entity instances in a break/stack have the same sample size, sample will be shown per break/stack
            //Case B: the entity instances in a break/stack have different sample sizes, but they are the same per instance across breaks/stacks, sample will be shown per instance
            //Case C: sample sizes are different both inside a break/stack and across the breaks/stacks, there isn't really a good way to show this other than showing
            //each (split instance + filter instance) individually so it will show none for now, it is possible to show them but there would be a lot

            if (samplePerInstance.All(s => HasSameEntitySampleSizes(s.SampleSizeMeta)))
            {
                return new SampleSizeMetadata
                {
                    SampleSize = SumSampleSizes(samplePerInstance.Select(i => i.SampleSizeMeta.SampleSize)),
                    SampleSizeByEntity = samplePerInstance.ToDictionary(s => s.EntityInstanceName, s => s.SampleSizeMeta.SampleSizeByEntity.First().Value),
                    CurrentDate = samplePerInstance.First().SampleSizeMeta.CurrentDate,
                    SampleSizeEntityInstanceName = sampleSizeEntityInstance?.Name
                };
            }
            else if (HasSameEntitySampleSizes(samplePerInstance.Select(s => s.SampleSizeMeta)))
            {
                return new SampleSizeMetadata
                {
                    SampleSize = SumSampleSizes(samplePerInstance.Select(i => i.SampleSizeMeta.SampleSize)),
                    SampleSizeByEntity = samplePerInstance.First().SampleSizeMeta.SampleSizeByEntity,
                    CurrentDate = samplePerInstance.First().SampleSizeMeta.CurrentDate,
                    SampleSizeEntityInstanceName = sampleSizeEntityInstance?.Name
                };
            }

            return null;
        }

        private static bool HasSameEntitySampleSizes(SampleSizeMetadata sampleSizeMeta)
        {
            var firstEntitySampleSize = sampleSizeMeta.SampleSizeByEntity.First().Value;
            return sampleSizeMeta.SampleSizeByEntity.Values.All(v => v == firstEntitySampleSize);
        }

        private static bool HasSameEntitySampleSizes(IEnumerable<SampleSizeMetadata> sampleSizeMetas)
        {
            var firstSampleSizeMeta = sampleSizeMetas.First();
            return sampleSizeMetas.Skip(1)
                .All(sampleSizeMeta => HasSameEntitySampleSizes(firstSampleSizeMeta, sampleSizeMeta));
        }

        private static bool HasSameEntitySampleSizes(SampleSizeMetadata a, SampleSizeMetadata b)
        {
            var sampleA = a.SampleSizeByEntity;
            var sampleB = b.SampleSizeByEntity;
            return sampleA.Count == sampleB.Count &&
                sampleA.Keys.All(key => sampleB.ContainsKey(key) && sampleA[key] == sampleB[key]);
        }

        private static UnweightedAndWeightedSample SumSampleSizes(IEnumerable<UnweightedAndWeightedSample> sampleSizes)
        {
            double unweighted = 0;
            double weighted = 0;

            foreach (var sample in sampleSizes)
            {
                unweighted += sample.Unweighted;
                weighted += sample.Weighted;
            }

            return new UnweightedAndWeightedSample
            {
                Unweighted = unweighted,
                Weighted = weighted
            };
        }

        private static UnweightedAndWeightedSample SumSampleSizes(IEnumerable<WeightedDailyResult?> weightedDailyResults)
        {
            double unweighted = 0;
            double weighted = 0;

            foreach (var result in weightedDailyResults)
            {
                unweighted += result?.UnweightedSampleSize ?? 0;
                weighted += result?.WeightedSampleSize ?? 0;
            }

            return new UnweightedAndWeightedSample
            {
                Unweighted = unweighted,
                Weighted = weighted
            };
        }
    }
}
