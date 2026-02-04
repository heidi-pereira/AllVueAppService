namespace BrandVue.PublicApi.Models
{
    public class QuestionValueAnswer : QuestionAnswer, IEquatable<QuestionValueAnswer>
    {
        /// <summary>
        /// Deprecated
        /// </summary>
        public int MinValue { get; set; }
        /// <summary>
        /// Deprecated
        /// </summary>
        public int MaxValue { get; set; }
        public double Multiplier { get; set; }

        public QuestionValueAnswer()
        {
            AnswerType = AnswerTypeEnum.Value;
        }

        public bool Equals(QuestionValueAnswer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && MinValue == other.MinValue && MaxValue == other.MaxValue && Multiplier.Equals(other.Multiplier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QuestionValueAnswer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ MinValue.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxValue.GetHashCode();
                hashCode = (hashCode * 397) ^ Multiplier.GetHashCode();
                return hashCode;
            }
        }
    }
}