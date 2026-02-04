using System.Threading.Tasks;

namespace BrandVue.SourceData.CalculationPipeline;

public interface ITextCountCalculatorFactory
{
    Task<ITextCountCalculator> CreateAsync();
}