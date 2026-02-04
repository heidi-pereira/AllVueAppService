namespace BrandVue.PublicApi.Models
{
    public class ClassInstanceDescriptor : IEquatable<ClassInstanceDescriptor>
    {
        /// <summary>
        /// The id of the class instance
        /// </summary>
        public int ClassInstanceId { get; }

        /// <summary>
        /// The name of the class instance
        /// </summary>
        public string Name { get; }

        public ClassInstanceDescriptor(int classInstanceId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            ClassInstanceId = classInstanceId;
            Name = name;
        }

        public bool Equals(ClassInstanceDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ClassInstanceId == other.ClassInstanceId && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClassInstanceDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return ClassInstanceId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{nameof(ClassInstanceId)}: {ClassInstanceId}, {nameof(Name)}: {Name}";
        }
    }
}