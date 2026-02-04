using BrandVue.PublicApi.ModelBinding;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    [ModelBinder(typeof(ClassModelBinder))]
    public class ClassDescriptor : IEquatable<ClassDescriptor>
    {
        /// <summary>
        /// Class id - used as the class identifier for calls to other API endpoints
        /// </summary>
        public string ClassId { get; }

        /// <summary>
        /// Class name - The name of the class. Often but not always the same as the class id.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Child class id's - A list of other class id's nested under this class.
        /// </summary>
        [Obsolete("Class combinations can be found on metrics and questions")]
        public string[] ChildClassIds { get; }

        internal EntityType EntityType { get; }

        [JsonConstructor] //Only use in tests which make an http call
        public ClassDescriptor(string classId, string name, string[] childClassIds)
        {
            if (string.IsNullOrWhiteSpace(classId))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(classId));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            ClassId = classId.SanitizeUrlSegment();;
            Name = name;
            ChildClassIds = childClassIds ?? throw new ArgumentNullException(nameof(childClassIds));
        }

        public ClassDescriptor(EntityType entityType, string[] childClassIds) : this(entityType.Identifier, entityType.Identifier, childClassIds)
        {
            EntityType = entityType;
        }

        internal bool IsProduct => Name.Equals("product", StringComparison.OrdinalIgnoreCase);

        public bool Equals(ClassDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && ChildClassIds.SequenceEqual(other.ChildClassIds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClassDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public static implicit operator EntityType(ClassDescriptor descriptor)
        {
            return descriptor.EntityType;
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }

    public static class ClassDescriptorExtensions
    {
        public static IEnumerable<EntityType> GetRequestEntityCombination(this IEnumerable<ClassDescriptor> classDescriptors)
        {
            foreach (var possiblyNullClassDescriptor in classDescriptors)
            {
                if (possiblyNullClassDescriptor is {} classDescriptor && !classDescriptor.EntityType.IsProfile)
                {
                    yield return classDescriptor.EntityType;
                }
            }
        }
    }
}