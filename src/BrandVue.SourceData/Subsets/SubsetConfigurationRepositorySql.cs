using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BrandVue.SourceData.Subsets
{
    public class SubsetConfigurationRepositorySql : ISubsetConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly IChoiceSetReader _choiceSetReader;
        private readonly ISubsetRepository _subsetRepository;

        public SubsetConfigurationRepositorySql(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext, IChoiceSetReader choiceSetReader, ISubsetRepository subsetRepository)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _choiceSetReader = choiceSetReader;
            _subsetRepository = subsetRepository;
        }

        public IReadOnlyCollection<SubsetConfiguration> GetAll()
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            return SubsetConfigurationsForProductContext(ctx).ToList();
        }

        private IQueryable<SubsetConfiguration> SubsetConfigurationsForProductContext(MetaDataContext ctx)
        {
            return ctx.SubsetConfigurations.Where(s => s.ProductShortCode == _productContext.ShortCode && s.SubProductId == _productContext.SubProductId);
        }

        public SubsetConfiguration Create(SubsetConfiguration modelToCreate, string identifier)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            var dbSubsetConfiguration = new SubsetConfiguration
            {
                Identifier = identifier
            };
            ApplyConfigurationModel(dbSubsetConfiguration, modelToCreate);
            ValidateModel(dbSubsetConfiguration);
            ctx.SubsetConfigurations.Add(dbSubsetConfiguration);
            ctx.SaveChanges();
            return dbSubsetConfiguration;
        }

        public void Update(SubsetConfiguration subsetConfiguration, int id)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            var subsetToUpdate = SubsetConfigurationsForProductContext(ctx).Single(f => f.Id == id);
            ApplyConfigurationModel(subsetToUpdate, subsetConfiguration);
            ValidateModel(subsetToUpdate);
            ctx.SubsetConfigurations.Update(subsetToUpdate);
            ctx.SaveChanges();
        }

        public void Delete(int subsetId)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            var subsetToDelete = SubsetConfigurationsForProductContext(ctx)
                .SingleOrDefault(f => f.Id == subsetId) ?? throw new BadRequestException("subset id could not be found in the database");
            ctx.SubsetConfigurations.Remove(subsetToDelete);
            ctx.SaveChanges();
        }

        private void ApplyConfigurationModel(SubsetConfiguration inModel, SubsetConfiguration modelToCreate)
        {
            if (inModel.Identifier != modelToCreate.Identifier) {
                throw new BadRequestException("Not allowed to change Identifier here");
            }
            inModel.SurveyIdToAllowedSegmentNames = modelToCreate.SurveyIdToAllowedSegmentNames;
            inModel.Description = modelToCreate.Description;
            inModel.Disabled = modelToCreate.Disabled;
            inModel.DisplayName = modelToCreate.DisplayName;
            inModel.DisplayNameShort = modelToCreate.DisplayNameShort;
            inModel.EnableRawDataApiAccess = modelToCreate.EnableRawDataApiAccess;
            inModel.Iso2LetterCountryCode = modelToCreate.Iso2LetterCountryCode;
            inModel.Order = modelToCreate.Order;
            inModel.ProductShortCode = _productContext.ShortCode;
            inModel.SubProductId = _productContext.SubProductId;
            inModel.Alias = modelToCreate.Alias;
            inModel.OverriddenStartDate = modelToCreate.OverriddenStartDate;
            inModel.AlwaysShowDataUpToCurrentDate = modelToCreate.AlwaysShowDataUpToCurrentDate;
            inModel.ParentGroupName = modelToCreate.ParentGroupName;
        }

        private record class SurveySegment(int SurveyId, string SegmentName)
        {
            public override string ToString()
            {
                return $"Survey {SurveyId}-{SegmentName}";
            }
        }
        private class SurveySegmentComparer : IEqualityComparer<SurveySegment>
        {
            public bool Equals(SurveySegment x, SurveySegment y)
            {
                return x.SurveyId == y.SurveyId && string.Compare(x.SegmentName, y.SegmentName, StringComparison.InvariantCultureIgnoreCase) == 0;
            }

            public int GetHashCode([DisallowNull] SurveySegment obj)
            {
                return obj.GetHashCode();
            }
        }

        private void ValidateModel(SubsetConfiguration subsetConfiguration)
        {
            if (string.IsNullOrEmpty(subsetConfiguration.Identifier))
                throw new BadRequestException($"{nameof(subsetConfiguration.Identifier)} cannot be null or empty.");

            if (string.IsNullOrEmpty(subsetConfiguration.DisplayName))
                throw new BadRequestException($"{nameof(subsetConfiguration.DisplayName)} cannot be null or empty.");

            var validSegments = _choiceSetReader
                .GetSegments(_productContext.NonMapFileSurveyIds)
                .Select(s => new SurveySegment(s.SurveyId, s.SegmentName));
            var invalidSegments = new List<SurveySegment>();
            foreach (var keyValuePair in subsetConfiguration.SurveyIdToAllowedSegmentNames)
            {
                foreach (var segmentName in keyValuePair.Value)
                {
                    var segment = new SurveySegment(keyValuePair.Key, segmentName);
                    if (!validSegments.Contains(segment, new SurveySegmentComparer()))
                    {
                        invalidSegments.Add(segment);
                    }
                }
            }

            if (invalidSegments.Any())
                throw new BadRequestException($"Non-existent survey segment(s)  - { string.Join(", ", invalidSegments) }");

            var invalidSurveyIds = subsetConfiguration.SurveyIdToAllowedSegmentNames.Keys.Except(_productContext.NonMapFileSurveyIds).ToArray();
            if (invalidSurveyIds.Any())
            {
                var builder = new StringBuilder();
                var missingSurveyIds = string.Join(", ", invalidSurveyIds);
                if (invalidSurveyIds.Length == 1)
                {
                    builder.Append($"Survey id {missingSurveyIds} is not accessible to survey {(_productContext.IsSurveyGroup ? "group" : "")} \"{_productContext.SubProductId}\".");
                }
                else
                {
                    builder.Append($"Survey ids ({missingSurveyIds}) are not accessible to survey {(_productContext.IsSurveyGroup ? "group" : "")} \"{_productContext.SubProductId}\".");

                }
                if (_productContext.IsSurveyGroup)
                {
                    var nameForSurvey = invalidSurveyIds.Length == 1 ? "this survey" : "these surveys";
                    builder.Append($" Consider adding {nameForSurvey} into the survey group \"{_productContext.SubProductId}\" via FieldVue.");
                    if (invalidSurveyIds.Length == 1)
                    {
                        builder.Append($" At the same time make sure that survey {missingSurveyIds} is assigned the same company.");
                    }
                    else
                    {
                        builder.Append($" At the same time make sure that these surveys ({missingSurveyIds}) are assigned the same company.");
                    }
                }
                else
                {
                    builder.Append($" Inorder to access multiple surveys in a single group you will need to create a survey group via FieldVue.");
                }
                builder.Append($" Link: https://docs.savanta.com/internal/Content/Research_Portal/Managing_Survey_Groups.html");
                throw new BadRequestException(builder.ToString());
            }

            using var ctx = _dbContextFactory.CreateDbContext();
            var subsetConfigsNotEditing = SubsetConfigurationsForProductContext(ctx)
                .Where(s => s.Id != subsetConfiguration.Id);
            bool displayNameInDb = subsetConfigsNotEditing.Any(s => s.ParentGroupName == subsetConfiguration.ParentGroupName && s.DisplayName == subsetConfiguration.DisplayName);
            bool identifierInDb = subsetConfigsNotEditing.Any(s => s.ParentGroupName == subsetConfiguration.ParentGroupName && s.Identifier == subsetConfiguration.Identifier);

            bool alreadyInMemory = _subsetRepository
                .Where(s => s.ParentGroupName == subsetConfiguration.ParentGroupName && s.Id != subsetConfiguration.Identifier)
                .Select(s => s.DisplayName)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
                .Contains(subsetConfiguration.DisplayName);

            if (displayNameInDb || identifierInDb || alreadyInMemory)
                throw new BadRequestException("displayName already in use.");
        }
    }

}
