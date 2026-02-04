using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.AnswersMetadata
{
    public class ChoiceSetReader : IChoiceSetReader
    {
        private readonly ILogger _logger;
        private readonly IAnswerDbContextFactory _answerDbContextFactory;
        private readonly Dictionary<HashSet<int>, (IReadOnlyCollection<Question> questions, IReadOnlyCollection<ChoiceSetGroup> choiceSets)> _cache = new(HashSet<int>.CreateSetComparer());

        public ChoiceSetReader(AppSettings appSettings, ILogger<ChoiceSetReader> logger) : this(appSettings.ConnectionString, logger)
        {
        }

        public ChoiceSetReader(string connectionString, ILogger logger) : this(new AnswerDbContextFactory(connectionString), logger)
        {
        }

        public ChoiceSetReader(IAnswerDbContextFactory answerDbContextFactory, ILogger logger)
        {
            _logger = logger;
            _answerDbContextFactory = answerDbContextFactory;
        }

        protected virtual AnswersDbContext BuildContext(int? commandTimeout = null)
        {
            return _answerDbContextFactory.CreateDbContext(commandTimeout);
        }

        public int[] GetSegmentIds(Subset subset)
        {
            if (subset.SurveyIdToSegmentNames?.Any() != true)
            {
                return Array.Empty<int>();
            }

            var surveySegments = GetSegments(subset.SurveyIdToSegmentNames.Keys);
            var subsetNameComparer = StringComparer.InvariantCultureIgnoreCase;
            var allowedSegmentNames = subset.AllowedSegmentNames.ToImmutableHashSet(subsetNameComparer);

            return surveySegments
                .Where(s => subset.SurveyIdToSegmentNames[s.SurveyId].EmptyOrContains(s.SegmentName, subsetNameComparer))
                .Where(s => allowedSegmentNames.IsEmpty || allowedSegmentNames.Contains(s.SegmentName))
                .Select(s => s.SurveySegmentId)
                .ToArray();
        }

        public bool SurveyHasNonTestCompletes(IEnumerable<int> surveyIds)
        {
            using var dbContext = BuildContext();
            var responses = from response in dbContext.SurveyResponses
                            join segment in dbContext.SurveySegments on response.SegmentId equals segment.SurveySegmentId
                            where surveyIds.Contains(response.SurveyId) && segment.SegmentName != "Test"
                            select new { response };
        
            return responses.Any();
        }

        /// <remarks>
        /// Sometimes choices aren't populated when looking at ancestor choice sets for example.
        /// Only for rare use - otherwise do a query to load the choiceset into memory to avoid db round trips.
        /// </remarks>
        public IReadOnlyList<int> GetSurveyChoiceIds(ChoiceSet choiceSet)
        {
            if (choiceSet.Choices != null)
            {
                return choiceSet.Choices.Select(c => c.SurveyChoiceId).ToArray();
            }
            var choiceSetId = choiceSet.ChoiceSetId;
            using var dbContext = _answerDbContextFactory.CreateDbContext();
            return dbContext.Choices.Where(c => c.ChoiceSetId == choiceSetId).Select(c => c.SurveyChoiceId).ToArray();
        }

        public virtual IEnumerable<SurveySegment> GetSegments(IEnumerable<int> surveyIds)
        {
            using var dbContext = _answerDbContextFactory.CreateDbContext();
            return (from segment in dbContext.SurveySegments
                        where surveyIds.Contains(segment.SurveyId) && segment.SegmentName != "Test" //Never include test responses, the data isn't in the data warehouse db anyway
                        select new SurveySegment{ SurveyId = segment.SurveyId, SurveySegmentId = segment.SurveySegmentId, SegmentName = segment.SegmentName })
                        .AsEnumerable().ToArray();
        }
        public void InvalidateCache(IEnumerable<int> surveyIds)
        {
            var hashSetOfSurveyIds = new HashSet<int>(surveyIds);
            if (_cache.ContainsKey(hashSetOfSurveyIds))
            {
                _cache.Remove(hashSetOfSurveyIds);
            }
        }
        public (IReadOnlyCollection<Question> questions, IReadOnlyCollection<ChoiceSetGroup> choiceSets) GetChoiceSetTuple(IReadOnlyCollection<int> surveyIds)
        {
            var hashSetOfSurveyIds = new HashSet<int>(surveyIds);
            if (_cache.TryGetValue(hashSetOfSurveyIds, out var cached))
            {
                return cached;
            }

            
            var result = (GetQuestions(hashSetOfSurveyIds), GetChoiceSetGroups(hashSetOfSurveyIds));
            _cache.Add(hashSetOfSurveyIds, result);
            return result;
        }

        private IReadOnlyCollection<ChoiceSetGroup> GetChoiceSetGroups(HashSet<int> surveyIds)
        {
            // For now increase the command timeout on this query whilst we move to Azure
            // This can revert once database is moved hopefully
            using var dbContext = BuildContext((int)TimeSpan.FromMinutes(4).TotalSeconds);

            //Get the latest choiceset id and choice id in common from the set of survey ids. We group on choiceset name and survey choice id
            //PERF: This reduces the number of rows from this query. Barometer initial load returned ~1,000,000 rows before and ~22,000 after.

            //Query 1: Pick canonical (latest) choicesetid for choice set name
            var csNameToCanonicalCsId =
                (from cs in dbContext.ChoiceSets.Where(cs => surveyIds.Contains(cs.SurveyId))
                group cs by cs.Name // Case-insensitive grouping
                into g
                select new { ChoiceSetName = g.Key, CanonicalChoiceSetId = g.Max(cs => cs.ChoiceSetId) }).ToArray();

            //Query 2: Pick canonical (latest) choice for every surveychoiceid with the same canonical set
            var choices = dbContext.Choices
                .AsNoTracking()
                .Include(c => c.ChoiceSet)
                .Where(c => surveyIds.Contains(c.SurveyId))
                .ToList(); // Materialize the query

            var canonicalChoiceChoiceSetTuples =
                (from c in choices
                join nameToCanonicalCsId in csNameToCanonicalCsId on c.ChoiceSet.Name equals nameToCanonicalCsId.ChoiceSetName
                group c by new { nameToCanonicalCsId.CanonicalChoiceSetId, c.SurveyChoiceId }
                into g
                select new
                {
                    CanonicalChoiceSetId = g.Key.CanonicalChoiceSetId,
                    CanonicalChoice = g.OrderByDescending(c => c.ChoiceId).First()
                }).ToArray();

            var canonicalChoiceSetIds = canonicalChoiceChoiceSetTuples.Select(x => x.CanonicalChoiceSetId).Distinct().ToArray(); 
            
            //Query 3: Get choice sets, as base for merging in canonical choices from all related sets
            var choiceSets = dbContext.ChoiceSets
                .Include(cs => cs.ParentChoiceSet1)
                .Include(cs => cs.ParentChoiceSet2)
                .AsNoTrackingWithIdentityResolution().Where(cs =>
                surveyIds.Contains(cs.SurveyId)).ToArray();

            // choice sets joined to csNameToCanonicalCsId
            var csNameToCanonicalCs = csNameToCanonicalCsId
                .Join(choiceSets, x => x.CanonicalChoiceSetId, y => y.ChoiceSetId, (canonical, choiceSet) => new
                {
                    canonical.ChoiceSetName,
                    ChoiceSet = choiceSet
                }).ToDictionary(x => x.ChoiceSetName, x => x.ChoiceSet, StringComparer.OrdinalIgnoreCase); // because we group case-insensitively in sql server above

            var canonicalChoiceSets = canonicalChoiceChoiceSetTuples
                .GroupBy(c => c.CanonicalChoiceSetId)
                .Join(choiceSets, c => c.Key, cs => cs.ChoiceSetId,
                    (g, canonicalChoiceSet) =>
                    {
                        // adding the parent choice sets to the canonical choice set
                        canonicalChoiceSet.ParentChoiceSet1 = canonicalChoiceSet.ParentChoiceSet1?.Name is { } name1 ? csNameToCanonicalCs[name1] : null;
                        canonicalChoiceSet.ParentChoiceSet2 = canonicalChoiceSet.ParentChoiceSet2?.Name is { } name2 ? csNameToCanonicalCs[name2] : null;
                        canonicalChoiceSet.ParentChoiceSet1Id = canonicalChoiceSet.ParentChoiceSet1?.ChoiceSetId;
                        canonicalChoiceSet.ParentChoiceSet2Id = canonicalChoiceSet.ParentChoiceSet2?.ChoiceSetId;

                        canonicalChoiceSet.Choices = g.Select(m => m.CanonicalChoice).ToList();
                        foreach(var c in canonicalChoiceSet.Choices)
                        {
                            c.ChoiceSetId = canonicalChoiceSet.ChoiceSetId;
                            c.ChoiceSet = canonicalChoiceSet;
                        }
                        return canonicalChoiceSet;
                    }
                ).OrderBy(cs => cs.ChoiceSetId);

            var choiceSetGroups = GetChoiceSetGroups(canonicalChoiceSets.ToArray(), _logger);

            return choiceSetGroups;
        }

        private IReadOnlyCollection<Question> GetQuestions(HashSet<int> surveyIds)
        {

            using var dbContext = BuildContext((int)TimeSpan.FromMinutes(4).TotalSeconds);
            return dbContext.Questions
                .Include(q => q.SectionChoiceSet)
                .Include(q => q.PageChoiceSet)
                .Include(q => q.QuestionChoiceSet)
                .Include(q => q.AnswerChoiceSet)
                .Where(q => surveyIds.Contains(q.SurveyId))
                .ToArray();
        }

        public IReadOnlyCollection<AnswerStat> GetAnswerStats(IReadOnlyCollection<int> surveyIds,
            IReadOnlyCollection<int> segmentIds)
        {
            if ((segmentIds == null || segmentIds.Count == 0))
            {
                return new List<AnswerStat>().ToArray();
            }
            using var dbContext = BuildContext((int)TimeSpan.FromMinutes(4).TotalSeconds);

            return dbContext.GetAnswerStats(segmentIds, surveyIds);
        }

        /// <summary>
        /// The entity sets tab can then continue as-is, using the friendly id defined in the entities tab. Medium will move into the UI so they can just select from the known entities.
        /// Choice sets with no parents are the original ancestors which explicitly list choices.
        /// All other choices sets contain the union of their parents' choices.
        /// Group choice sets based on their ancestry OR if they're an exact copy paste name and id of every choice.
        /// </summary>
        /// <param name="allChoiceSets"></param>
        /// <param name="logger">Temporary logger to try to diagnose allvue issue</param>
        /// <returns></returns>
        internal static ChoiceSetGroup[] GetChoiceSetGroups(IEnumerable<ChoiceSet> allChoiceSets, ILogger logger = null)
        {
            var equivalentChoiceSets =
                allChoiceSets.ToDisjointGroups(ChoiceSetAncestryComparer.Instance, ChoiceSetNameIdComparer.Instance);

            var choiceSetGroups =
                equivalentChoiceSets.Select(g =>
                    {
                        if (g.Count() > 1 && logger != null)
                        {
                            logger.LogInformation($"For choice set (ID: {g.Key.ChoiceSetId}, Name: {g.Key.Name}), found matching sets: {string.Join(", ", g.Select(g => g.Name))}");
                        }

                        return new ChoiceSetGroup(Canonical(g), g.Select(x => x).ToArray());
                    })
                    .OrderByDescending(c => c.Canonical.Choices.Count)
                    .ToArray();

            return choiceSetGroups.ToArray();

            // TODO: Deal with edge cases
            // * Choice sets with #PAGECHOICEID# in the name
            // * Special options like "none", "all", "other" are used - could parent entity sets in brandvue

        }

        private static ChoiceSet Canonical(IGrouping<ChoiceSet, ChoiceSet> g)
        {
            return g.OrderBy(x => x.SurveyId).ThenBy(x => x.FollowMany(PriorAncestors).Count()).ThenBy(x => x.ChoiceSetId).First();
        }

        private static IEnumerable<ChoiceSet> PriorAncestors(ChoiceSet c)
        {
            if (c.ParentChoiceSet1 != null) yield return c.ParentChoiceSet1;
            if (c.ParentChoiceSet2 != null) yield return c.ParentChoiceSet2;
        }
    }
}
