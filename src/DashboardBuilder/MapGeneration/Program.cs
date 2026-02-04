using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Metrics;
using MIG.SurveyPlatform.MapGeneration.Model;
using MIG.SurveyPlatform.MapGeneration.Mqml;
using MIG.SurveyPlatform.MapGeneration.Serialization;
using MIG.SurveyPlatform.MapGeneration.Serialization.Model;

namespace MIG.SurveyPlatform.MapGeneration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2) Console.Error.WriteLine(GetUsageExamples());
            else GenerateFields(args);
        }

        private static string GetUsageExamples()
        {
            return $"Usage:\r\n{nameof(MapGeneration)} z:\\eatingout.mqml Consumer_segment|Shopper_segment [-OutputFilePath c:\\outputDir\\GeneratedMap.xlsx] [-SubsetChoiceSetName Key_products_bought_show] [-Force]\r\n" +
                   $"If the OutputFilePath chosen already exists, the generated sheets will be added with a unique name to avoid overwriting existing sheets. To overwrite them instead, use the -Force switch";
        }

        private static void GenerateFields(string[] args)
        {
            var mqmlFilename = args.ElementAt(0);
            var consumerSegmentQuestionNames = args.ElementAt(1).Split('|');
            var subsetChoiceSetNames = GetArgValue(args, "-SubsetChoiceSetName")?.Split('|') ?? new string[0];
            var outputFilePath = GetArgValue(args, "-OutputFile") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Generated/Map_{DateTime.Now:yyyy-MM-ddThh-mm-ss}.xlsx");
            var force = args.Contains("-Force", StringComparer.OrdinalIgnoreCase);
            new Program().GenerateFields(mqmlFilename, consumerSegmentQuestionNames, subsetChoiceSetNames, outputFilePath, force);
        }

        private static string GetArgValue(string[] args, string argName)
        {
            return args.SkipWhile(a => !a.Equals(argName, StringComparison.OrdinalIgnoreCase)).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// * The result of asking a question is one or more "fields"
        /// * BrandFields come from questions which reference a brand. Profiling fields cover all other questions.
        /// * Metrics (displayed in BrandVue) are fields can be derived from both types of fields.
        /// * For a BrandVue tracker, most metrics are derived from a single brand field, the generator focuses on generating these metrics.
        /// * The information required to specify a field is almost wholly contained in the mqml except:
        /// * There are a few details about context that change how we'd display the data. For example if a question says "from that same brand, what's your favourite product". A human knows that is a brand field related to the brand in the previous question, it's harder for a computer to consistently get that correct. Fortunately this is very rare.
        /// * There are cases where what users want to see is bucketed together differently from how we ask the question. Here we prefer to generate more possibilities and let a person just delete the ones they don't want. If the number of possibilities is very large (e.g. over 50), then a few examples is sufficient and a person can copy paste and tweak for the ones they want.
        /// </summary>
        public void GenerateFields(string mqmlFilename, string[] consumerSegmentQuestionNames, string[] subsetChoiceSetNames, string outputFilepath, bool force)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilepath));
            var mapData = WithTransformedFields(mqmlFilename, MqmlReader.CreateMapData(mqmlFilename, consumerSegmentQuestionNames, subsetChoiceSetNames));
            Console.WriteLine($"Using question {string.Join(", ", consumerSegmentQuestionNames.Select(c => $@"'{c}'"))} to identify brand fields");
            if (subsetChoiceSetNames.Any()) Console.WriteLine($"Using choice set {string.Join(", ", subsetChoiceSetNames.Select(c => $@"'{c}'"))} to identify subsets");

            var metricDefinitions = new MetricGenerator(mapData).Generate();
            var allPages = DashPagesGenerator.CreateRootPage(metricDefinitions).OrderedDescendants.ToList();
            var allPanes = allPages.SelectMany(page => page.Panes).ToList();
            var allParts = allPanes.SelectMany(pane => pane.Parts).ToList();

            var sheets = new ISerializableEnumerable[]
            {
                new SubsetSerializationInfo().For(mapData.Subsets),
                new ProfilingFieldSerializationInfo().For(mapData.FieldCollections.ProfileFields),
                new BrandFieldSerializationInfo().For(mapData.FieldCollections.BrandFields),
                new MetricDefinitionSerializationInfo().For(metricDefinitions),
                new DashPageSerializationInfo().For(allPages),
                new DashPaneSerializationInfo().For(allPanes),
                new DashPartSerializationInfo().For(allParts),
            };
            ExcelSerializer.Save(sheets, outputFilepath, force);

            Console.WriteLine("\r\nFields generated. Next you will need to:");
            Console.WriteLine($"* Open {outputFilepath}");
            Console.WriteLine("* Add sheets for settings, surveys, filters and brands");
            Console.WriteLine("* Check quota cell profile fields have correct names and ranges for BV to connect to filters/weightings");
            Console.WriteLine($"* Save {outputFilepath} somewhere more useful and report any bugs in this generator on the BrandVue Dashboard Teams channel");
            if (mapData.Subsets.Count() > 1) Console.Write($"* To the Subset column of the Surveys sheet, add {string.Join("|", mapData.Subsets.Select(s => s.Id))}");
        }

        /// <summary>
        /// This is the only bit of the app that's allowed to do survey-specific things.
        /// The aim is for this method to be empty, and the logic to be generalized and incorporated into the main flow.
        /// </summary>
        private MapData WithTransformedFields(string mqmlFilename, MapData mapData)
        {
            if (!mqmlFilename.Contains("Financ")) return mapData;

            var profileMetrics = mapData.FieldCollections.ProfileFields.OfType<FieldDefinition>().ToDictionary(f => f.Name);
            profileMetrics["Regions"].Context.HumanBaseName = "Region";
            SetCategories(profileMetrics, "Age", "16-24:16-24|25-34:25-34|35-49:35-49|50-59:50-59|60-74:60-74");
            SetCategories(profileMetrics, "Gender", "0:Female|1:Male");
            SetCategories(profileMetrics, "Regions", "1-2:Northern Ireland and Scotland|3-5:North|6-8:Midlands|9-11:South|12:London");
            SetCategories(profileMetrics, "SEG1", "1-2:AB|3:C1|4,8:C2|5-7:DE|9:Retired");
            SetCategories(profileMetrics, "SEG2", "1-2:AB|3:C1|4,8:C2|5-7:DE");
            return mapData;
        }

        private static void SetCategories(Dictionary<string, FieldDefinition> profileMetrics, string name, string categoriesText)
        {
            profileMetrics[name].Categories = categoriesText;
        }
    }
}
