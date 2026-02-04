namespace BrandVue.PublicApi.Models
{
    public class QuestionDescriptor : IEquatable<QuestionDescriptor>
    {
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public QuestionAnswer AnswerSpec { get; set; }
        public string[] Classes { get; set; }

        public bool Equals(QuestionDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(QuestionId, other.QuestionId) && 
                   string.Equals(QuestionText, other.QuestionText) && 
                   Equals(AnswerSpec, other.AnswerSpec) &&
                   Classes.OrderBy(s => s).SequenceEqual(other.Classes.OrderBy(s => s));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((QuestionDescriptor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (QuestionId != null ? QuestionId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (QuestionText != null ? QuestionText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AnswerSpec != null ? AnswerSpec.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Classes != null ? Classes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(QuestionId)}: {QuestionId}, {nameof(QuestionText)}: {QuestionText}, {nameof(AnswerSpec)}: {AnswerSpec}, {nameof(Classes)}: {string.Join("|", Classes)}";
        }
    }
}
