using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData;

namespace Test.BrandVue.SourceData.TimestampMassage
{
    public class DateRange
    {
        public DateRange(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            StartDate = startDate.ToDateInstance();
            EndDate = endDate.ToDateInstance();
        }

        public DateTimeOffset StartDate { get; private set; }
        public DateTimeOffset EndDate { get; private set; }
    }
}
