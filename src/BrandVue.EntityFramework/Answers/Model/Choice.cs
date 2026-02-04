using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BrandVue.EntityFramework.Answers.Model;

[DebuggerDisplay("{SurveyChoiceId}: {Name}")]
public class Choice
{
    public int ChoiceId { get; set; }
    [Required]
    public int ChoiceSetId { get; set; }
    [Required]
    public ChoiceSet ChoiceSet { get; set; }
    [Required]
    public int SurveyId { get; set; }
    [Required]
    public int SurveyChoiceId { get; set; }
    [Required, MaxLength(500)]
    public string Name { get; set; }
    [MaxLength(1024)]
    public string ImageURL { get; set; }
}
public static class ChoiceExtensions
{
    public static string GetDisplayName(this Choice choice)
    {
        return string.IsNullOrEmpty(choice.Name) ? choice.ImageURL: choice.Name;
    }
}