using System.ComponentModel.DataAnnotations;

namespace OpenEnds.BackEnd.Model
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string VarCode { get; set; }
    }
}
