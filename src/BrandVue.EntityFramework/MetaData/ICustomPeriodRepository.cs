using System.Text;

namespace BrandVue.EntityFramework.MetaData
{
    public interface ICustomPeriodRepository
    { 
        IReadOnlyCollection<CustomPeriod> GetAllFor(string productShortCode, string organisation, string subProductId);
    }
}
