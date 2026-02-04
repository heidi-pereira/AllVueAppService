namespace BrandVue.PublicApi.Models
{
    public class QuestionMultipleChoiceAnswer : QuestionAnswer, IEquatable<QuestionMultipleChoiceAnswer>
    {
        public IEnumerable<QuestionChoice> Choices { get; set; }

        public QuestionMultipleChoiceAnswer()
        {
            AnswerType = AnswerTypeEnum.Category;
        }

        public bool Equals(QuestionMultipleChoiceAnswer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Choices.SequenceEqual(other.Choices);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QuestionMultipleChoiceAnswer) obj);
        }

        public override int GetHashCode()
        {
            return (Choices != null ? Choices.GetHashCode() : 0);
        }
    }
}