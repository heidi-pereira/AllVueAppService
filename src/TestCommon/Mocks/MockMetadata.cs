using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace TestCommon.Mocks
{
    public static class MockMetadata
    {
        public static EntityInstance[] CreateEntityInstances(ushort count)
        {
            return Enumerable.Range(1, count).Select(i => new EntityInstance { Id = i, Name = i.ToString() }).ToArray();
        }

        public static IGroupedQuotaCells CreateNonInterlockedQuotaCells(Subset subset, ushort count)
        {
            var regions = new[] { "L", "N", "M", "S" };
            return GroupedQuotaCells.CreateUnfiltered(Enumerable.Range(0, count)
                .Select(i => new QuotaCell(i, subset, QuotaCell.DefaultCellDefinition(regions[i], "0", "0", "0"))));
        }

        public static IQuotaCellReferenceWeightingRepository CreateQuotaCellReferenceWeightingRepository(Subset subset, (QuotaCell QuotaCell, WeightingValue Weight)[] weightings)
        {
            var quotaCellReferenceWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            quotaCellReferenceWeightingRepository.Get(subset).Returns(CreateWeightings(subset, weightings));
            return quotaCellReferenceWeightingRepository;
        }

        public static QuotaCellReferenceWeightings CreateWeightings(Subset subset,
            (QuotaCell QuotaCell, WeightingValue Weight)[] valueTuples)
        {
            return new QuotaCellReferenceWeightings(valueTuples.ToDictionary(w => w.QuotaCell.ToString(), w => w.Weight));
        }
    }
}