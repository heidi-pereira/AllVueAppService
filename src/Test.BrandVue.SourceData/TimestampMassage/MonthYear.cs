using System;

namespace Test.BrandVue.SourceData.TimestampMassage
{
    internal class MonthYear : IComparable, IComparable<MonthYear>
    {
        private static readonly string[] Months =
        {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec"
        };

        public MonthYear(int month, int year)
        {
            Month = month;
            Year = year;
        }

        public int Month { get; private set; }
        public int Year { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as MonthYear;
            return other != null && ToString().Equals(other.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return CompareTo((MonthYear) obj);
        }

        public int CompareTo(MonthYear other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other),
                    "Cannot compare if other is null.");
            }

            var result = Year.CompareTo(other.Year);
            return result == 0 ? Month.CompareTo(other.Month) : result;
        }

        public override string ToString()
        {
            return $"{Months[Month - 1]}-{Year}";
        }
    }
}
