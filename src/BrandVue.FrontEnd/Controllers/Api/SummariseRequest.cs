using System.ComponentModel.DataAnnotations;
namespace BrandVue.Controllers.Api;

public class SummariseRequest
{
    [Required]
    [StringLength(2000000, MinimumLength = 10, ErrorMessage = "The text must be between 10 and 2,000,000 characters long.")]
    public string Text { get; set; }

    public bool UseGemini { get; set; } = false;

    public string BrowserLanguage { get; set; } = "";
}
