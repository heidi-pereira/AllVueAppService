using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.ResponseRepository
{
    public class WeightedWordCount
    {
        [Key]
        public string Text { get; set; }
        public double Result { get; set; }
        public int UnweightedResult { get; set; }
    }
}