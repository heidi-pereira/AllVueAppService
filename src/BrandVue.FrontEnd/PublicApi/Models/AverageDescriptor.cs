using BrandVue.PublicApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{

    [ModelBinder(typeof(AverageModelBinder))]
    public class AverageDescriptor : IEquatable<AverageDescriptor>
    {
        internal SourceData.Averages.AverageDescriptor Average { get; }
        /// <summary>
        /// The id of the average
        /// </summary>
        public string AverageId { get; }

        /// <summary>
        /// The name of the average
        /// </summary>
        public string Name { get; }

        [JsonConstructor] //Only use in tests which make an http call
        public AverageDescriptor(string averageId, string name)
        {
            AverageId = averageId ?? throw new ArgumentNullException(nameof(averageId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public AverageDescriptor(SourceData.Averages.AverageDescriptor average) : this(average.AverageId, average.DisplayName)
        {
            Average = average;
        }

        public override string ToString()
        {
            return $"{nameof(AverageId)}: {AverageId}, {nameof(Name)}: {Name}";
        }

        public bool Equals(AverageDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(AverageId, other.AverageId) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AverageDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (AverageId != null ? AverageId.GetHashCode() : 0);
        }

        public bool IsCustom() => Average.IsCustom();

        public static implicit operator SourceData.Averages.AverageDescriptor(AverageDescriptor descriptor)
        {
            return descriptor.Average;
        }
    }
}