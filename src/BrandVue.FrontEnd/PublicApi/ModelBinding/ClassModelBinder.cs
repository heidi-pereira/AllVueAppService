using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    internal class ClassModelBinder : RawStringModelBinderBase<ClassDescriptor>
    {
        protected override ClassDescriptor BindModel(ModelBindingContext bindingContext, string classString)
        {
            var classDescriptor = bindingContext.HttpContext.GetService<IClassDescriptorRepository>()
                .ValidClassDescriptors()
                .SingleOrDefault(c => c.ClassId.Equals(classString, StringComparison.OrdinalIgnoreCase));

            if (classDescriptor != null) return classDescriptor;

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{classString}' is not a recognized class, use the classes endpoint to discover available classes");
            return null;
        }
    }
}