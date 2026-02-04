using System;
using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.Data.FieldMapping;
using MIG.SurveyPlatform.MapGeneration.Model;
using ReportingSystem;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal class FieldCollectionsFactory
    {
        /// <remarks>
        /// There are likely cases where other variables are used, but this is the main one using the special inbuilt variable
        /// </remarks>
        private const string PageChoiceIdSuffix = "_#PAGECHOICEID#";
        private readonly ExportMaker m_ExportMaker;
        private readonly ILookup<string, FieldMutator> m_FieldMutatorsByQuestionName;
        private readonly HashSet<string> m_AbstractUsedBrandContextFields;
        private readonly ILookup<string, int?> m_ChoiceIdsForVarRoot;

        public FieldCollectionsFactory(ExportMaker exportMaker, ILookup<string, FieldMutator> fieldMutatorsByQuestionName,
            IEnumerable<BrandContextTag> brandContextTags)
        {
            m_ExportMaker = exportMaker;
            m_FieldMutatorsByQuestionName = fieldMutatorsByQuestionName;
            m_AbstractUsedBrandContextFields = new HashSet<string>(brandContextTags.Select(t => t.BrandIdFieldPrefix + PageChoiceIdSuffix));
            m_ChoiceIdsForVarRoot = exportMaker.AllFields.Values.ToLookup(FieldForOutputExtensions.GetVarRoot, fo => fo.Field?.ChoiceID);
        }

        public FieldCollections CreateFieldCollections()
        {
            var profileFieldsWithoutRepeatedTags = m_ExportMaker.AllNonRepeated.Values
                .ToLookup(GetPossibleBaseName)
                .Where(IsUsedOrNonRepeating)
                .SelectMany(x => x.Key == null ? x : x.Where(fo => !fo.Question.OriginalNameOrVarCode.EndsWith(PageChoiceIdSuffix)));
            var brandFields = m_ExportMaker.AllRepeated.Values.Concat(m_ExportMaker.AllUsingBrand.Values);

            var fields = profileFieldsWithoutRepeatedTags.SelectMany(pf => CreateMutatedFieldDefinitions(pf, false))
                .Concat(brandFields.SelectMany(bf => CreateMutatedFieldDefinitions(bf, true))).ToList();

            var textFieldsUsed = new HashSet<string>(fields.Where(f => !string.IsNullOrEmpty(f.BrandIdTag) || !string.IsNullOrEmpty(f.ProfileField))
                .SelectMany(f => new[] {f.BrandIdTag, f.ProfileField}));

            //For now, brandvue can't handle text fields
            var fieldsToOutput = fields
                .Where(f => f.Type != "Text" || textFieldsUsed.Contains(f.Name) || textFieldsUsed.Contains(GetPossibleBaseName(f.Name)))
                .ToLookup(f => f.IsBrandField);

            return new FieldCollections(fieldsToOutput[false], fieldsToOutput[true]);
        }

        private bool IsUsedOrNonRepeating(IGrouping<string, ExportMaker.FieldForOutput> g)
        {
            return g.Key == null || g.All(fo => !fo.Question.OriginalNameOrVarCode.EndsWith(PageChoiceIdSuffix) || IsAbstractUsedBrandContextTag(fo));
        }

        /// <summary>
        /// By abstract tag I mean it still containing a variable name like #PAGECHOICEID#
        /// </summary>
        private bool IsAbstractUsedBrandContextTag(ExportMaker.FieldForOutput arg)
        {
            return m_AbstractUsedBrandContextFields.Contains(arg.Question.OriginalNameOrVarCode);
        }

        private static string GetPossibleBaseName(ExportMaker.FieldForOutput fo)
        {
            return fo.Question.IsTagField ? GetPossibleBaseName(fo.Question.OriginalNameOrVarCode) : null;
        }

        private static string GetPossibleBaseName(string varCode)
        {
            return varCode.Contains("_")
                ? varCode.Substring(0, varCode.LastIndexOf("_") + 1)
                : null;
        }

        private IEnumerable<FieldDefinition> CreateMutatedFieldDefinitions(ExportMaker.FieldForOutput fo, bool isRepeated)
        {
            var fieldMutators = m_FieldMutatorsByQuestionName[fo.Question.OriginalNameOrVarCode].ToList();

            if (!fieldMutators.Any() || fieldMutators.Any(fm => fm.ShouldOutputBaseField))
            {
                var baseFieldDef = CreateBaseFieldDefinition(fo, isRepeated);
                AddBrandFieldDetails(fo, baseFieldDef);
                yield return baseFieldDef;
            }

            foreach (var fieldMutator in fieldMutators
                .OrderBy(f => f.ProfileFieldName).ThenBy(f => f.ProfileFieldValue).ThenBy(f => f.FieldSuffix).ThenBy(f => f.BaseNameSuffix).ThenBy(f => f.UsageId))
            {
                var fieldDef = CreateBaseFieldDefinition(fo, fieldMutator.IsBrandField ?? isRepeated);
                fieldDef.Field += fieldMutator.FieldSuffix;
                fieldDef.UsageId = fieldMutator.UsageId ?? fieldDef.UsageId;
                fieldDef.Context.HumanNameSuffix = fieldMutator.BaseNameSuffix.Humanize();
                fieldDef.ProfileField = fieldMutator.ProfileFieldName;
                fieldDef.ProfileValues = fieldMutator.ProfileFieldValue;
                fieldDef.HasSubsetNumericSuffix = fieldMutator.HasSubsetNumericSuffix;
                fieldDef.BrandIdTag = fieldMutator.BrandIdTag;
                AddBrandFieldDetails(fo, fieldDef);
                yield return fieldDef;
            }
        }

        private FieldDefinition CreateBaseFieldDefinition(ExportMaker.FieldForOutput fo, bool isBrandField)
        {
            var varRoot = fo.GetVarRoot();
            var usageId = GetUsageIdOrNull(fo);
            var categories = (fo.Field?.Bands ?? new List<DataBand>()).Select(b => b.AsText).ToList();
            var page = fo.Question.Parent;
            var scaleSpecs = fo.Question.GetScaleSpecs();
            var useScale = scaleSpecs != null && scaleSpecs.Min < scaleSpecs.Max;
            var fieldContext = new FieldContext
            {
                PageName = page.OriginalName ?? page.Name,
                SectionName = page.Parent.OriginalName ?? page.Parent.Name,
                Categories = categories,
                QuestionType = fo.Question.MainOptType(),
                HumanBaseName = fo.OriginalName.Humanize(),
                ScaleMin = useScale ? scaleSpecs.Min : 0,
                ScaleMax = useScale ? scaleSpecs.Max : 1,
                IsImplicitlyAskedToEveryoneWhoSeesBrand = false,
                IsRatingQuestion = useScale && fo.Question.TextsContain("Rate ", StringComparison.Ordinal) || fo.Question.TextsContain(" rate ", StringComparison.OrdinalIgnoreCase) || fo.Question.TextsContain("on a scale of", StringComparison.OrdinalIgnoreCase)
            };
            var fieldDef = new FieldDefinition(fieldContext)
            {
                IsBrandField = isBrandField,
                Field = varRoot,
                Type = DescribeDataType(fo),
                UsageId = usageId,
                //Note: For ranges (e.g. Age), this will return nothing because bands is null and will need to be filled in manually depending on the desired quota cells.
                Categories = string.Join("|", fieldContext.Categories),
                Question = string.Join(" ", fo.Question.QuestionReplaced.Split('\r', '\n')),
                ParentChoiceSet = fo.Field?.ParentChoiceSet
            };
            return fieldDef;
        }

        private int? GetUsageIdOrNull(ExportMaker.FieldForOutput fo)
        {
            return m_ChoiceIdsForVarRoot[fo.GetVarRoot()].Count() > 1 ? fo.Field.ChoiceID : (int?) null;
        }

        private static void AddBrandFieldDetails(ExportMaker.FieldForOutput fo, FieldDefinition fieldDef)
        {
            if (fo.BrandQType != ExportMaker.BrandQTypes.None)
            {
                var hasBrandSuffix = fo.BrandQType == ExportMaker.BrandQTypes.SectionLevel || fo.BrandQType == ExportMaker.BrandQTypes.PageLevel;
                var varCodeSuffix = fo.BrandQType == ExportMaker.BrandQTypes.SectionLevel ? "_" : "";
                fieldDef.HasBrandSuffix = hasBrandSuffix;
                fieldDef.Field += varCodeSuffix;
                fieldDef.FieldName = hasBrandSuffix ? fieldDef.Name.Replace(fo.GetVarRoot() + varCodeSuffix, "") : "#BRAND#";
            }
        }

        public static string DescribeDataType(ExportMaker.FieldForOutput fo)
        {
            switch (fo.Question.MainOptType())
            {
                case QQuestion.MainOptTypes.Radio:
                case QQuestion.MainOptTypes.Combo:
                    return fo.Field?.Bands == null ? "Value" : "Category1";
                case QQuestion.MainOptTypes.CheckBox:
                case QQuestion.MainOptTypes.Value:
                    return "Value";
                case QQuestion.MainOptTypes.Text:
                    return fo.Question.OriginalName.StartsWith("Spontaneous_awareness", StringComparison.OrdinalIgnoreCase) ? "BrandText" : "Text";
                default:
                    throw new ArgumentOutOfRangeException(nameof(fo), fo, null);
            }
        }
    }
}