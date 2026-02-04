using System.ComponentModel.DataAnnotations;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class AverageTotalRequestModel
    {
        public AverageTotalRequestModel(string averageName, CuratedResultsModel requestModel = null)
        {
            AverageName = averageName;
            RequestModel = requestModel;
        }

        [Required]
        public string AverageName { get; }
        public CuratedResultsModel RequestModel { get; }
    }
}
