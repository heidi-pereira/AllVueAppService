using BrandVue.EntityFramework.MetaData.ReportVue;
using Newtonsoft.Json;
using System.IO;

namespace BrandVue.Services.ReportVue
{
    public class DScorecardCell
    {
        public int RowId { get; set; } = 0;
        public int ColumnId { get; set; } = 0;

        public string DisplayText { get; set; }
        public double Value { get; set; } = 0;
        public double Sample { get; set; } = 0;
    }

    public class DScorecardRow
    {
        public enum ScorecardRowTypes
        {
            Unknown = 0,
            Main = 1,
            Title = 2,
            Gap = 3,
            BrandTitle = 4,
            Group = 5,
            GroupTitle = 6,
            GroupSubTitle = 7,
            Image = 8,
            ImageFile = 10,
            SpanTitle = 11,
            DataTitle = 12,
            ColumnRank = 13,
            RowGroupTitle = 14,
            SimpleTitle = 15,
            VariableTitle = 16,
            DifferenceDescription = 17
        }

        public int Id { get; set; }

        public string Text { get; set; }

        public ScorecardRowTypes RowType { get; set; }

        public List<DScorecardCell> Cells { get; set; } = new List<DScorecardCell>();
    }

    public class DScoreCard
    {
        public List<DScorecardRow> Rows { get; set; } = new List<DScorecardRow>();
        public static DScoreCard Load(string fileName)
        {
            var jsonText = File.ReadAllText(fileName);
            var scoreCard = JsonConvert.DeserializeObject<DScoreCard>(jsonText);
            return scoreCard;
        }

    }

    public class PageTag
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ZoneContent
    {
        public string ContentType { get; set; }
        public string Value { get; set; }
    }

    public class PageContent
    {
        public int Id { get; set; }
        public int ReportFilterId { get; set; }
        public int BrandId { get; set; }
        public List<PageTag> Tags { get; set; }
        public List<ZoneContent> ZoneContents { get; set; }
        
    }

    public class Brand
    {
        public int Id { get; set; }
        public string BrandName { get; set; }
        public string DisplayName { get; set; }
        public string Name=> DisplayName?? BrandName;
    }
    public class ReportPage
    {
        public int Id { get; set; }
        public string PageTitle { get; set; }
    }
    public class ReportSection
    {
        public string Name { get; set; }
        public ReportPage[] ReportPages { get; set; }
    }

    public class DashboardBuildParameters
    {
        public string DesktopToolsVersion { get; set; }
        public string Template { get; set; }
        public DateTime? LastUpdateTemplateDateTime { get; set; }
        public DateTime? LastUpdateDataVueDateTime { get; set; }
        public DateTime? GenerationDateTime { get; set; }
    }

    public class BrandDefinition
    {
        public string SingularTitle { get; set; }
        public string PlurarlTitle { get; set; }
    }
    public class FilterTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class ReportVueReport
    {
        public string DashboardTitle { get; set; }
        public int SlideWidth { get; set; }
        public int SlideHeight { get; set; }
        public List<PageContent> PageContents { get; set; }
        public Brand[] BrandRecords { get; set; }
        public List<ReportSection> ReportSections { get; set; }
        public List<FilterTemplate> FilterTemplates { get; set; }

        public BrandDefinition BrandSettings { get; set; }
        public DashboardBuildParameters DashboardBuildParameters { get; set; }

        public static ReportVueReport Load(string fileName)
        {
            var jsonText = File.ReadAllText(fileName);
            var report = JsonConvert.DeserializeObject<ReportVueReport>(jsonText);
            if (report != null)
            {
                report.DashboardBuildParameters = JsonConvert.DeserializeObject<DashboardBuildParameters>(jsonText);
            }
            return report;
        }
    }

    public record SpecificQuestion(string PageTitle, int BrandId, int ColumnOfData);
    public static class ReportVueReportExtensions
    {
        public static string GetResultsForSpecificQuestions(this ReportVueReport reportVueReport, SpecificQuestion[] questions, string pathToFiles)
        {
            var values = new List<string>();
            foreach (var question in questions)
            {
                foreach (var section in reportVueReport.ReportSections)
                {
                    var reportPages = section.ReportPages.Where(x => x.PageTitle == question.PageTitle);
                    foreach (var reportPage in reportPages)
                    {
                        var matchingPageContent = reportVueReport.PageContents.SingleOrDefault(x => x.Id == reportPage.Id && x.BrandId == question.BrandId);
                        if (matchingPageContent != null)
                        {
                            var scorecardZone = matchingPageContent.ZoneContents.SingleOrDefault(x => x.ContentType == "Scorecard");
                            if (scorecardZone != null)
                            {
                                var fileToRead = Path.Combine(pathToFiles, scorecardZone.Value);
                                var scoreCard = DScoreCard.Load(fileToRead);
                                values.Add(reportVueReport.BrandRecords.Single(x=>x.Id == question.BrandId).Name);
                                foreach (var scorecardRow in scoreCard.Rows)
                                {
                                    if (scorecardRow.RowType != DScorecardRow.ScorecardRowTypes.Gap && (scorecardRow.Cells.Count > question.ColumnOfData) && scorecardRow.Cells[question.ColumnOfData].Sample > 0)
                                    {
                                        if (scorecardRow.Cells[question.ColumnOfData].DisplayText.Length > 1)
                                        {
                                            values.Add(scorecardRow.Cells[question.ColumnOfData].DisplayText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return string.Join(",", values);
        }

        public static ReportVueProjectRelease ConvertToReportVueProjectRelease(this ReportVueReport reportVueReport)
        {
            var root = new ReportVueProjectRelease();
            root.UserTextForBrandEntity = reportVueReport.BrandSettings?.SingularTitle??"Brand";
            foreach (var page in reportVueReport.PageContents)
            {
                var newProjectPage = page.Convert(reportVueReport);
                if (root.ProjectPages.Any(x => x.Id == newProjectPage.Id && x.BrandId == newProjectPage.BrandId && x.FilterId == newProjectPage.FilterId && x.PageId == newProjectPage.PageId))
                {
                    //ToDo: Need to do something here
                }
                else
                {
                    root.ProjectPages.Add(newProjectPage);
                }
            }
            return root;
        }
        private static ReportVueProjectPage Convert(this PageContent pageContent, ReportVueReport parentReport)
        {
            var projectPage = new ReportVueProjectPage();
            projectPage.BrandId = pageContent.BrandId;
            projectPage.PageId = pageContent.Id;
            projectPage.FilterId = pageContent.ReportFilterId;

            projectPage.BrandName = parentReport.BrandRecords.First(x=> x.Id == projectPage.BrandId).Name;
            projectPage.FilterName = parentReport.FilterTemplates[projectPage.FilterId].Name;

            projectPage.PageName = "???";
            projectPage.SectionName = "???";

            foreach (var section in parentReport.ReportSections)
            {
                var matchingPage = section.ReportPages.SingleOrDefault(x => x.Id == pageContent.Id);
                if (matchingPage!= null)
                {
                    projectPage.PageName = matchingPage.PageTitle;
                    projectPage.SectionName = section.Name;
                }
            }

            var tags = pageContent.Tags.Select(x => {
                var tag = x as PageTag;
                return new ReportVueProjectPageTag() { TagName = tag.Name, TagValue = tag.Value };
                }
            ).ToList();

            tags.AddRange(pageContent.ZoneContents.Where(x => x.ContentType == "Text").Select(x => new ReportVueProjectPageTag() { TagName = x.ContentType, TagValue = x.Value }));

            projectPage.Tags.AddRange(tags);
            return projectPage;
        }
    }
}
