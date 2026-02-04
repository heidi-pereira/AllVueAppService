using BrandVue.EntityFramework;
using BrandVue.Models;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    [ModelBinder(typeof(SurveysetModelBinder))]
    public class SurveysetDescriptor : IEquatable<SurveysetDescriptor>, ISubsetIdProvider
    {
        internal Subset Subset { get; }

        [JsonConstructor] //Only use in tests which make an http call
        public SurveysetDescriptor(string surveysetId, string name)
        {
            SurveysetId = surveysetId;
            Name = name;
        }

        public SurveysetDescriptor(Subset subset) : this(subset.Alias, subset.DisplayName)
        {
            Subset = subset;
        }

        /// <summary>
        /// Surveyset id - used as the surveyset identifier for calls to other API endpoints
        /// </summary>
        public string SurveysetId { get; }
        /// <summary>
        /// Surveyset name - a description of the surveyset.  Often but not always the same as the surveyset id.
        /// </summary>
        public string Name { get; }

        public bool Equals(SurveysetDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SurveysetId, other.SurveysetId) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SurveysetDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (SurveysetId != null ? SurveysetId.GetHashCode() : 0);
        }

        public static implicit operator Subset(SurveysetDescriptor descriptor)
        {
            return descriptor.Subset;
        }

        public override string ToString()
        {
            return $"{nameof(Subset)}: {Subset}, {nameof(SurveysetId)}: {SurveysetId}, {nameof(Name)}: {Name}";
        }

        string ISubsetIdProvider.SubsetId => Subset.Id;
    }
}