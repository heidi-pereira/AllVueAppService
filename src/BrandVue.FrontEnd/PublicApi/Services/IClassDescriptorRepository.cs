using BrandVue.PublicApi.Models;

namespace BrandVue.PublicApi.Services
{
    public interface IClassDescriptorRepository
    {
        IReadOnlyCollection<ClassDescriptor> ValidClassDescriptors();
    }
}