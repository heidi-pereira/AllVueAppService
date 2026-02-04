using BrandVue.PublicApi.Extensions;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.SourceData.Models.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    [ModelBinder(typeof(MetricModelBinder))]
    public class MetricDescriptor : IEquatable<MetricDescriptor>
    {
        internal SourceData.Measures.Measure Measure { get; }

        /// <summary>
        /// The id of the metric
        /// </summary>
        public string MetricId { get; }

        /// <summary>
        /// The name of the metric
        /// </summary>
        public string Name { get; }
        public string Description { get; }

        /// <summary>
        /// The type of the metric. This will be a class id.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The filter for the metric
        /// </summary>
        public FilterInfo Filter { get; }

        /// <summary>
        /// The base filter for the metric
        /// </summary>
        public FilterInfo BaseFilter { get; }

        /// <summary>
        /// The start date for the metric
        /// </summary>
        public DateTimeOffset? StartDate { get; }

        /// <summary>
        /// The classes for this metric
        /// </summary>
        public string[] QuestionClasses { get; set; }

        [JsonConstructor]
        public MetricDescriptor(string metricId, string name, string description, FilterInfo filter, FilterInfo baseFilter, DateTimeOffset? startDate, string type, string[] classes)
        {
            MetricId = metricId;
            Name = name;
            Description = description;
            Filter = filter;
            BaseFilter = baseFilter;
            StartDate = startDate;
            Type = type;
            QuestionClasses = classes;
        }

        public MetricDescriptor(SourceData.Measures.Measure measure) : 
            this(measure.UrlSafeName, measure.Name, measure.HelpText, measure.PrimaryDisplayInfo, 
                measure.BaseDisplayInfo, measure.StartDate, measure.GetMeasureType(),
                measure.GetMeasureClasses())
        {
            Measure = measure;
        }

        public static implicit operator SourceData.Measures.Measure(MetricDescriptor descriptor) => descriptor.Measure;

        public bool Equals(MetricDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(MetricId, other.MetricId) &&
                   string.Equals(Name, other.Name) &&
                   Equals(Filter, other.Filter) &&
                   Equals(BaseFilter, other.BaseFilter) &&
                   StartDate.Equals(other.StartDate) &&
                   Type.Equals(other.Type) &&
                   QuestionClasses.OrderBy(s => s).SequenceEqual(other.QuestionClasses.OrderBy(s => s));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MetricDescriptor) obj);
        }

        public override int GetHashCode() => (MetricId != null ? MetricId.GetHashCode() : 0);

        public override string ToString() => 
            $"{nameof(Name)}: {Name}, {nameof(Filter)}: {Filter}, {nameof(BaseFilter)}: {BaseFilter}, {nameof(StartDate)}: {StartDate}, {nameof(Type)}: {Type}, {nameof(QuestionClasses)}: {string.Join("|", QuestionClasses)}";

    }
}