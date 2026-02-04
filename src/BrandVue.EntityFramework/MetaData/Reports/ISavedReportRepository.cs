namespace BrandVue.EntityFramework.MetaData.Reports
{
    public interface ISavedReportRepository
    {
        IReadOnlyCollection<SavedReport> GetAll();
        IReadOnlyCollection<SavedReport> GetFor(string userId);
        SavedReport GetDefault();
        void UpdateReportIsDefault(int reportId, bool isDefault);
        bool IsDefault(SavedReport report);
        void Create(SavedReport report);
        void Update(SavedReport report);
        void Delete(int reportId);
        SavedReport GetById(int reportId);
    }
}
