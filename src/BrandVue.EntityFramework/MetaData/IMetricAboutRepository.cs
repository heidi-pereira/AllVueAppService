using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData
{
    public interface IMetricAboutRepository
    {
        IEnumerable<MetricAbout> GetAllForMetric(string metricName);
        MetricAbout Get(int id);
        void Create(MetricAbout metricAbout);
        void Update(MetricAbout metricAbout);
        void UpdateList(MetricAbout[] metricAbouts);
        void Delete(MetricAbout metricAbout);
    }
}
