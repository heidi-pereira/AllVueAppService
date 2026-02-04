using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    /// <summary>
    /// Stores basic profile (demographic, SEG) and brand response data for an individual survey respondent.
    /// PERF/MEM/ALLOC: This class and the classes it calls are the most blazingly hot code paths in the codebase
    ///     Everything in this class except the constructor will be executed one or more times per respondent - hudreds of thousands of even millions of times per request.
    ///     i.e. Adding just 0.001ms to a code path here could make a request take a whole second longer - we aim to keep requests under 2 seconds in the worst case.
    ///     Optimized code is often harder to read and requires understanding details of dot net. Take your time to learn about it, pair with someone and get a review from someone who understands the examples in this comment.
    ///     Changes should be based on profiling and a before/after comparison using the YesNoSingleResultBenchmarks
    ///
    ///     Examples:
    ///         Pre-allocating an empty array for each field in eatingout could take 1GB of memory https://app.shortcut.com/mig-global/story/64213/performance-answer-retrieval#activity-64323
    ///         Using ToArray would cause an allocation which slows down execution directly, but also indirectly via GC pressure cleaning up so many small objects https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
    ///         More subtly: using a lambda that captures a value creates a compiler-generated class each usage, so causes the same issue.
    ///            You can apply the "static" keyword to a lambda to get the compiler to check for this: "static x => x * x" https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions#capture-of-outer-variables-and-variable-scope-in-lambda-expressions
    ///         Changing a class to struct *may* allow an object beneath the 24 bytes minimum object size header and you won't need an 8 byte reference to it. https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/
    ///         Boxing a struct nay incur the object costs just mentioned https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing
    /// </summary>
    public sealed class ProfileResponseEntity : BaseIdentifiable, IProfileResponseEntity
    {
        private readonly object _fieldIndexToValuesWriteLock = new();
        // Don't be tempted to use ConcurrentDictionaries unless you have to here as they use up a lot more memory with little gain in performance
        private IResponsesForField[] _fieldIndexToValues = Array.Empty<IResponsesForField>();

        public ProfileResponseEntity(int id, DateTimeOffset timestamp, int surveyId)
        {
            Id = id;
            Timestamp = timestamp;
            SurveyId = surveyId;
        }

        public void AddFieldValue(ResponseFieldDescriptor field, EntityIds entityIds, int value, Subset subset)
        {
            var entitiesToValue = GetOrAddResponsesForField(field, subset);
            entitiesToValue.Add(entityIds, value);
        }

        private IResponsesForField GetOrAddResponsesForField(ResponseFieldDescriptor field, Subset subset)
        {
            if (!TryGetResponsesForField(field, out var entitiesToValue))
            {
                entitiesToValue = CreateNewResponsesForField(field, subset);
                int index = field.LoadOrderIndex;

                // Any potential readers of the *new* data will be waiting on RespondentMeasureDataLoader._dataLockObject. The existing data will be unmodified so can be read without a lock.
                lock (_fieldIndexToValuesWriteLock)
                {
                    if (index >= _fieldIndexToValues.Length)
                    {
                        var newSize = index + (int)Math.Max(4, index * 0.1);
                        Array.Resize(ref _fieldIndexToValues, index + newSize);
                    }

                    _fieldIndexToValues[index] = entitiesToValue;
                }
            }
            return entitiesToValue;
        }

        private bool TryGetResponsesForField(ResponseFieldDescriptor field, out IResponsesForField entitiesToValue)
        {
            entitiesToValue = null;
            if (field.LoadOrderIndex < _fieldIndexToValues.Length)
            {
                entitiesToValue = _fieldIndexToValues[field.LoadOrderIndex];
            }

            return entitiesToValue is not null;
        }

        private static IResponsesForField CreateNewResponsesForField(ResponseFieldDescriptor responseFieldDescriptor, Subset subset)
        {
            var dataAccessModel = responseFieldDescriptor.GetDataAccessModel(subset.Id);
            IResponsesForField responsesForField = dataAccessModel.OrderedEntityCombination.Any()
                ? new ResponsesForField()
                : new ZeroEntityResponseForField();
            return (dataAccessModel?.IsAutoGenerated ?? false) && (dataAccessModel?.ScaleFactor).HasValue
                ? new ScaledResponsesForField(dataAccessModel.ScaleFactor.Value, responsesForField)
                : responsesForField;
        }

        public int? GetIntegerFieldValue(ResponseFieldDescriptor field, EntityValueCombination entityValues)
        {
            if (field is null || !TryGetResponsesForField(field, out var entitiesToValue)) return null;
            if (entitiesToValue.TryGetValue(entityValues.EntityIds, out var value))
            {
                return value;
            }

            return null;
        }


        public Memory<T> GetIntegerFieldValues<T>(ResponseFieldDescriptor field,
            Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> memoryPool)
        {
            if (field is not null && TryGetResponsesForField(field, out var entitiesToValue))
            {
                return entitiesToValue.WriteWhere(predicate, select, memoryPool);
            }

            return Memory<T>.Empty;
        }

        private bool Equals(ProfileResponseEntity other)
        {
            return Id == other.Id && Timestamp == other.Timestamp;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ProfileResponseEntity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Id, Timestamp).GetHashCode();
        }

        public DateTimeOffset Timestamp { get; }

        public int SurveyId { get; }
    }
}