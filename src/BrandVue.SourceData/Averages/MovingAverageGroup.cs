//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BrandVue.SourceData.Calculation
//{
//    public class MovingAverageGroup : Attribute
//    {
//        public const string GroupNameAll = "All";
//        public const string GroupNameCalendar = "Calendar";
//        public const string GroupNameCalendarPeriodOnPeriod = "CalendarPeriodOnPeriod";

//        private static readonly string[] SupportedGroupNames =
//            {GroupNameAll, GroupNameCalendar, GroupNameCalendarPeriodOnPeriod};

//        public MovingAverageGroup(params string[] groupNames)
//        {
//            if (groupNames == null || groupNames.Length == 0)
//            {
//                throw new ArgumentException(
//                    $"Error initialising average group membership: {nameof(groupNames)} may not be null or empty.",
//                    nameof(groupNames));
//            }

//            groupNames.Where(groupName => !SupportedGroupNames.Contains(groupName)).ToList().ForEach(
//                groupName => throw new ArgumentOutOfRangeException(
//                    nameof(groupNames),
//                    groupName,
//                    $"The average group name {groupName} is not supported.") );
//        }
//    }
//}
