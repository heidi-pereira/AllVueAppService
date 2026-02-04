using BrandVue.EntityFramework.MetaData.Breaks;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public record ReportWaveConfiguration
    {
        public ReportWavesOptions WavesToShow { get; set; }
        public int NumberOfRecentWaves { get; set; }
        [CanBeNull]
        public CrossMeasure Waves { get; set; }
    }
}
