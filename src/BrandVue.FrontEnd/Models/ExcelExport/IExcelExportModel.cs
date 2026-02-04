namespace BrandVue.Models.ExcelExport
{
    public interface IExcelExportModel
    {
        string Name { get; }
        ViewTypeEnum ViewType { get; }
        string LeadVisualization { get; }
    }
}
