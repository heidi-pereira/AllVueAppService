using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MIG.SurveyPlatform.Data;
using MIG.SurveyPlatform.Data.FieldMapping;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal class MqmlReader
    {
        private const string AllSubsetId = "All";
        private readonly QQuestionnaire m_Quest;
        private readonly ExportMaker m_ExportMaker;
        private readonly QChoiceSet[] m_SubsetChoiceSets;

        private MqmlReader(QQuestionnaire quest, ExportMaker exportMaker, QChoiceSet[] subsetChoiceSets)
        {
            m_Quest = quest;
            m_ExportMaker = exportMaker;
            m_SubsetChoiceSets = subsetChoiceSets;
        }

        public static MapData CreateMapData(string mqmlFilename, string[] consumerSegmentQuestionNames, string[] subsetChoiceSetNames)
        {
            var quest = ImportQuestionnaireWithPopulatedFields(mqmlFilename);
            var questions = consumerSegmentQuestionNames.Select(quest.QuestionFromName).ToList();
            var brandChoiceSets = questions
                .Select(q => GetChoiceSetByName(quest, q.ChoiceSetName)).ToList();
            var mainBrandChoiceSet = brandChoiceSets.First();
            var brandChoiceSetNames = quest.GetNamesOfRelatedChoiceSets(brandChoiceSets);
            var exportMaker = CreateExportMakerInner(quest, brandChoiceSetNames, mainBrandChoiceSet);
            var subsetChoiceSets = subsetChoiceSetNames.Select(s => GetChoiceSetByName(quest, s)).ToArray();
            var mqmlReader = new MqmlReader(quest, exportMaker, subsetChoiceSets);

            var subsets = GetSubsets(subsetChoiceSets);
            return new MapData
            {
                FieldCollections = mqmlReader.ReadFieldCollections(consumerSegmentQuestionNames, brandChoiceSetNames),
                BrandAskedQuestion = questions.First().VarCode,
                BrandAskedChoices = questions.First().OptionsChoiceSet.Choices.oList.Select(x => x.ID + ":" + x.Name).ToList(),
                Subsets = subsets
            };
        }

        private static QChoiceSet GetChoiceSetByName(QQuestionnaire quest, string choiceSetName)
        {
            var choiceSetFromName = quest.ChoiceSetFromName(choiceSetName);
            if (choiceSetFromName == null) throw new ArgumentOutOfRangeException(nameof(choiceSetName), choiceSetName, null);
            return choiceSetFromName;
        }

        private static IEnumerable<Subset> GetSubsets(QChoiceSet[] subsetChoiceSets)
        {
            var subsets = new[] {new Subset(AllSubsetId) {Id = AllSubsetId}}.Concat(subsetChoiceSets.SelectMany(cs => cs.Choices.oList
                .Select((s, i) => new Subset(s.Name) {NumericSuffix = s.ID.ToString(), Order = (i + 1) * 10})));
            return AssignUniqueIds(subsets.ToList());
        }

        private static IReadOnlyCollection<Subset> AssignUniqueIds(IReadOnlyCollection<Subset> subsets)
        {
            var duplicates = subsets.Where(s => s.Id != AllSubsetId).ToList();
            for(int dedupeSeverity = 0; duplicates.Any(); dedupeSeverity++)
            {
                foreach (var dupe in duplicates)
                {
                    dupe.Id = dupe.DisplayName.CreateAcronym(dedupeSeverity);
                }
                var lookup = subsets.ToLookup(s => s.Id);
                duplicates = lookup.Where(g => g.Count() > 1).SelectMany(d => d).ToList();
            }
            return subsets;
        }

        private FieldCollections ReadFieldCollections(string[] consumerSegmentQuestionNames, HashSet<string> brandChoiceSetNames)
        {
            var actionParser = new ActionParser(consumerSegmentQuestionNames, brandChoiceSetNames, m_SubsetChoiceSets);
            var brandContextTags = actionParser.GetBrandContextTags(m_Quest).ToList();
            var fieldMutatorsByQuestionName = actionParser.GetFieldMutators(m_Quest, brandContextTags)
                .ToLookup(fm => fm.ApplyToBaseVariableCode);

            return new FieldCollectionsFactory(m_ExportMaker, fieldMutatorsByQuestionName, brandContextTags)
                .CreateFieldCollections();
        }

        private static ExportMaker CreateExportMakerInner(QQuestionnaire quest, HashSet<string> otherChoiceSets, QChoiceSet mainChoiceSet)
        {
            var brandsChoiceSet = mainChoiceSet;
            var exportMaker = new ExportMaker(quest, mainChoiceSet, mainChoiceSet, string.Join("|", otherChoiceSets));
            exportMaker.GetAllFields();
            return exportMaker;
        }

        private static QQuestionnaire ImportQuestionnaireWithPopulatedFields(string mqmlFilename)
        {
            var quest = new QQuestionnaire();
            if (!quest.ImportFromXMLFromFile(mqmlFilename)) throw new FileNotFoundException($"Could not find `{mqmlFilename}`");

            // Populate the DNugget of each question.
            var completedIntStatusAsString = ((int)QQuestionnaire.CompletionStatus.Completed).ToString();
            var fakeSurveyIds = "1"; // Seems unlikely this can be used for anything since the database that knows about survey ids is not used in this code...as far as I know
            QQuestionnaire_DataHelper.PrepareForData(quest, fakeSurveyIds, completedIntStatusAsString, true, "", "");
            // Pretend there's data so that the field isn't skipped. This will mean we might add some fields that don't have data, but there's no harm in that and they're easily removed.
            QQuestionnaire_DataHelper.PrepareForData(quest, fakeSurveyIds, completedIntStatusAsString, true, "", "");

            return quest;
        }
    }
}