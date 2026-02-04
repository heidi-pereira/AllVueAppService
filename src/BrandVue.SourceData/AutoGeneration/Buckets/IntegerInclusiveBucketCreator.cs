namespace BrandVue.SourceData.AutoGeneration.Buckets
{
    public class IntegerInclusiveBucketCreator<TBucket> where TBucket : IntegerInclusiveBucket, new()
    {
        public IEnumerable<TBucket> CreateBuckets(int minimum, int maximum, int numberOfBuckets)
        {
            if (numberOfBuckets < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfBuckets), "IntegerInclusiveBucketCreator can not have a negative number of buckets");
            }
            
            var buckets = new List<TBucket>();

            if (numberOfBuckets == 0)
            {
                return buckets;
            }

            float bucketSize = (float)(Math.Abs(maximum - minimum) + 1) / (float)numberOfBuckets;
            
            for (int i = 0; i < numberOfBuckets; i++)
            {
                bool isInverted = maximum < minimum;
                int inclusiveBucketAdjustment = isInverted ? -1 : 1;
                int min = minimum + (inclusiveBucketAdjustment * (int) Math.Round(bucketSize * i, MidpointRounding.AwayFromZero));
                int max = minimum + (inclusiveBucketAdjustment * (int) Math.Round(bucketSize * (i + 1), MidpointRounding.AwayFromZero) - inclusiveBucketAdjustment);
                buckets.Add(isInverted
                    ? new TBucket {MinimumInclusive = max, MaximumInclusive = min}
                    : new TBucket {MinimumInclusive = min, MaximumInclusive = max});
            }

            return buckets;
        }

        public IEnumerable<TBucket> CreateBucketsForAge()
        {
            var buckets = new List<TBucket>();
            buckets.Add(new TBucket()
            {
                MaximumInclusive =24,
                MinimumInclusive = 18,
            });
            buckets.Add(new TBucket()
            {
                MaximumInclusive = 34,
                MinimumInclusive = 25,
            });
            buckets.Add(new TBucket()
            {
                MaximumInclusive = 44,
                MinimumInclusive = 35,
            });
            buckets.Add(new TBucket()
            {
                MaximumInclusive = 54,
                MinimumInclusive = 45,
            });
            buckets.Add(new TBucket()
            {
                MaximumInclusive = 64,
                MinimumInclusive = 55,
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 65,                
            });

            return buckets;
        }
        
        public IEnumerable<TBucket> CreateBucketsForNoOfChildren()
        {
            var buckets = new List<TBucket>();
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 0,
                MaximumInclusive = 0
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 1,
                MaximumInclusive = 1
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 2,
                MaximumInclusive = 2
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 3,
                MaximumInclusive = 3
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 4,
                MaximumInclusive = 4
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 5,
                MaximumInclusive = 5
            });
            buckets.Add(new TBucket()
            {
                MinimumInclusive = 6
            });
            return buckets;
        }
    }
}