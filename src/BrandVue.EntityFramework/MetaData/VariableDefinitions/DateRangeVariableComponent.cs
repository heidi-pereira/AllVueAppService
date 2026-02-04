using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class DateRangeVariableComponent : VariableComponent
    {
        private static readonly DateTime MinValidDate = new DateTime(2000, 1, 1);
        private static readonly DateTime MaxValidDate = new DateTime(2030, 1, 1);
        
        [Required]
        public DateTime MinDate { get; set; }
        
        [Required]
        public DateTime MaxDate { get; set; }
        public override bool IsValid(out string errorMessage)
        {
            var minDateInvalid = MinDate < MinValidDate || MinDate > MaxValidDate;
            var maxDateInvalid = MaxDate < MinValidDate || MaxDate > MaxValidDate;
            if (minDateInvalid || maxDateInvalid)
            {
                errorMessage = $"{nameof(MinDate)} and {nameof(MaxDate)} must be between {MinValidDate} and {MaxValidDate}";
                return false;
            }

            if (MinDate >= MaxDate)
            {
                errorMessage = $"{nameof(MinDate)} must be an earlier date than {nameof(MaxDate)}";
                return false;
            }

            if (MinDate.TimeOfDay > TimeSpan.Zero)
            {
                errorMessage = $"{nameof(MinDate)} must be at start of day (midnight). Data waves beginning in the middle of the day are not yet supported.";
                return false;
            }

            if (MaxDate.TimeOfDay != new TimeSpan(0, 23, 59, 59, 999))
            {
                errorMessage = $"{nameof(MaxDate)} must be at end of day (1 ms before midnight). Data waves ending in the middle of the day are not yet supported.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
