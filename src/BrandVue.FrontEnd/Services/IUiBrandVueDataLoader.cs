using BrandVue.SourceData.Dashboard;

namespace BrandVue.Services
{
    public interface IUiBrandVueDataLoader
    {
        IPagesRepository PageRepository { get; }
        IPanesRepository PaneRepository { get; }
        IPartsRepository PartRepository { get; }
    }
}