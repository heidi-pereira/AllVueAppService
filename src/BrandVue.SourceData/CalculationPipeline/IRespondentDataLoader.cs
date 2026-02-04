using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.SourceData.CalculationPipeline
{
    public interface IRespondentDataLoader
    {
        Task PossiblyLoadMeasures(IRespondentRepository respondentRepository, Subset subset,
            FieldsAndDataTargets targets, long startTicks, long endTicks,
            CancellationToken cancellationToken);
    }
}