using BrandVue.PublicApi.ModelBinding;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    [ModelBinder(typeof(VariableModelBinder))]
    public class VariableDescriptor : IEquatable<VariableDescriptor>
    {
        /// <summary>
        /// Variable id - used as the variable identifier for calls to other API endpoints
        /// </summary>
        public string VariableIdentifier { get; }

        /// <summary>
        /// Variable display name - The display name for the variable
        /// </summary>
        public string DisplayName { get; }

        [JsonConstructor] //Only use in tests which make an http call
        public VariableDescriptor(string variableIdentifier, string displayName)
        {
            if (string.IsNullOrWhiteSpace(variableIdentifier))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(variableIdentifier));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayName));
            VariableIdentifier = variableIdentifier;
            DisplayName = displayName;
        }

        public bool Equals(VariableDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(VariableIdentifier, other.VariableIdentifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VariableDescriptor)obj);
        }

        public override int GetHashCode()
        {
            return (VariableIdentifier != null ? VariableIdentifier.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{nameof(VariableIdentifier)}: {VariableIdentifier}";
        }
    }
}