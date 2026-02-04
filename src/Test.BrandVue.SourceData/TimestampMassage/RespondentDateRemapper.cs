using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrandVue.SourceData;
using BrandVue.SourceData.Respondents;

namespace Test.BrandVue.SourceData.TimestampMassage
{
    public class RespondentDateRemapper
    {
        private readonly IRespondentRepository _respondents;

        public RespondentDateRemapper(IRespondentRepository respondents)
        {
            _respondents = respondents;
        }

        public string RemapRespondentDates(DateRange source, DateRange target)
        {
            var toBeRemapped = _respondents.Where(
                respondent =>
                    respondent.ProfileResponseEntity.Timestamp.ToDateInstance() >= source.StartDate
                    && respondent.ProfileResponseEntity.Timestamp.ToDateInstance() <= source.EndDate)
                .OrderBy(respondent => respondent.ProfileResponseEntity.Id);

            var numberOfRespondents = toBeRemapped.Count();
            Console.WriteLine($"Remapping timestamps for {numberOfRespondents} responses.");

            var targetEndDate = target.EndDate.AddDays(1).ToDateInstance();

            var timeIncrement = new TimeSpan((targetEndDate - target.StartDate).Ticks / (numberOfRespondents + 1L));
            var newRespondentTimestamp = target.StartDate.Add(timeIncrement);   //  Ensure timestamp isn't exactly midnight.
            var buffer = new StringBuilder("ResponseID,NewTimestamp");
            buffer.Append(Environment.NewLine);

            foreach (var respondent in toBeRemapped)
            {
                buffer.Append(respondent.ProfileResponseEntity.Id);
                buffer.Append(',');
                //buffer.Append(newRespondentTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                buffer.Append(newRespondentTimestamp.ToString("yyyy-MM-dd"));
                buffer.Append(Environment.NewLine);

                newRespondentTimestamp = newRespondentTimestamp.Add(timeIncrement);
            }

            return buffer.ToString();
        }
    }
}
