using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Measures;
using NJsonSchema.Annotations;

namespace BrandVue.Variable
{
    public class VariableConfigurationCreateModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public VariableDefinition Definition { get; set; }

        [CanBeNull]
        public VariableConfigurationReportSettings ReportSettings { get; set; } = null;

        [CanBeNull]
        public CalculationType? CalculationType { get; set; } = null;

        [CanBeNull]
        public string? IdentifierOverride { get; set; } = null;
    }

    public class VariableConfigurationReportSettings
    {
        public int ReportIdToAppendTo { get; set; }
        [CanBeNull]
        public string? SelectedPart { get; set; }
        public ReportVariableAppendType AppendType { get; set; }
    }

    public enum ReportVariableAppendType
    {
        Part,
        Breaks,
        Base,
        Filters,
        Waves
    }
}