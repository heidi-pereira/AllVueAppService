using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Entity;

namespace BrandVue.PublicApi.Services
{
    public class ClassDescriptorRepository : IClassDescriptorRepository
    {
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;

        public ClassDescriptorRepository(IResponseEntityTypeRepository responseEntityTypeRepository)
        {
            _responseEntityTypeRepository = responseEntityTypeRepository;
        }

        /// <summary>
        /// Profile is NOT a class and should not be returned here.
        /// </summary>
        public IReadOnlyCollection<ClassDescriptor> ValidClassDescriptors()
        {
            return _responseEntityTypeRepository
                .Where(t => !t.IsProfile)
                .Select(t => new ClassDescriptor(t, Array.Empty<string>())).ToList();
        }
    }
}
