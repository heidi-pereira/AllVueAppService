namespace BrandVue.SourceData.CalculationPipeline
{
    public class TimeRangesManager
    {
        private readonly List<TimeRange> _timeRanges;

        internal IEnumerable<TimeRange> TimeRanges
        {
            get
            {
                lock (_timeRanges)
                {
                    return _timeRanges.ToList();
                }
            }
        }

        public TimeRangesManager() => _timeRanges = new List<TimeRange>();

        public TimeRangesManager(long startTicks, long endTicks)
        {
            _timeRanges = new List<TimeRange>()
            {
                new()
                {
                    Start = new DateTime(startTicks),
                    End = new DateTime(endTicks)
                }
            };
        }

        public bool IsRangeEntirelyIncluded(long start, long end)
        {
            return IsRangeEntirelyIncluded(new DateTime(start), new DateTime(end));
        }

        public bool IsRangeEntirelyIncluded(DateTime start, DateTime end)
        {
            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end), end,
                    $"{nameof(start)} ({start}) should be less than or equal to {nameof(end)} ({end})");
            }

            lock (_timeRanges)
            {
                return _timeRanges.Any(x => 
                    x.Start <= start && start <= x.End &&
                    x.Start <= end && end <= x.End
                );
            }
        }

        public void AddRange(long start, long end) => AddRange(new DateTime(start), new DateTime(end));

        public void AddRange(DateTime start, DateTime end)
        {
            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end), end,
                    $"{nameof(start)} ({start}) should be less than or equal to {nameof(end)} ({end})");
            }

            TimeRange GetMatchingRange(DateTime time) => _timeRanges.Find(x => x.Start <= time && time <= x.End);
            void RemoveTotallyOverlappedRanges() => _timeRanges.RemoveAll(t => start <= t.Start && t.End <= end);

            lock (_timeRanges)
            {
                var startOverlap = GetMatchingRange(start);
                var endOverlap = GetMatchingRange(end);
                if (startOverlap != null && startOverlap == endOverlap)
                {
                    return;
                }

                var newTimeRangeStart = start;
                var newTimeRangeEnd = end;
                if (startOverlap != null)
                {
                    newTimeRangeStart = startOverlap.Start;
                    _timeRanges.Remove(startOverlap);
                }

                if (endOverlap != null)
                {
                    newTimeRangeEnd = endOverlap.End;
                    _timeRanges.Remove(endOverlap);
                }

                RemoveTotallyOverlappedRanges();

                _timeRanges.Add(new TimeRange
                {
                    Start = newTimeRangeStart,
                    End = newTimeRangeEnd
                });
            }
        }

        public class TimeRange
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public override string ToString() => $"{nameof(Start)}: {Start}, {nameof(End)}: {End}";
        }
    }
}