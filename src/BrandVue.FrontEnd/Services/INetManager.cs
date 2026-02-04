using BrandVue.EntityFramework.MetaData;

namespace BrandVue.Services
{
    public interface INetManager
    {
        void RemoveNet(string selectedSubsetId, int partId, string metricName, int netVariableId, int optionToRemove);
        void CreateNewNet(string selectedSubsetId, MetricConfiguration metric, int partId, string netName, int[] nettedEntityInstanceIds);
        void AddGroupToNet(MetricConfiguration metric, int partId, string netName, ICollection<int> nettedEntityInstanceIds);
    }
}
