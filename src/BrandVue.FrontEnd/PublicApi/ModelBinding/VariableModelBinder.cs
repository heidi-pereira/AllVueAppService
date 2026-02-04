using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.Variable;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    internal class VariableModelBinder : RawStringModelBinderBase<VariableDescriptor>
    {
        protected override VariableDescriptor BindModel(ModelBindingContext bindingContext, string variableIdentifier)
        {
            var variable = bindingContext.HttpContext.GetService<IVariableConfigurationRepository>()
                .GetByIdentifier(variableIdentifier);

            if (variable != null) return new VariableDescriptor(variable.Identifier, variable.DisplayName);

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{variableIdentifier}' is not a recognized variable");//todo extend text here when variable endpoint is added
            return null;
        }
    }
}