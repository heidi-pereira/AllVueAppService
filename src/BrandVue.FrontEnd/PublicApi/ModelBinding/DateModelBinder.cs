using System.Globalization;
using BrandVue.SourceData;
using BrandVue.SourceData.QuotaCells;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    public class DateModelBinder : IModelBinder
    {
        public static readonly string DateModelBinderDateFormat = "yyyy-M-d";

        public static string RequestedDateOutOfDateRange(string strDateValue, DateTimeOffset startDate, DateTimeOffset endDate) =>
            $"{strDateValue} is out of responses date range. It should be between {startDate.Date} and {endDate.Date}";

        public Task BindModelAsync(ModelBindingContext generalBindingContext)
        {
            var bindingContext = (DefaultModelBindingContext) generalBindingContext;

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            if (!DateTimeOffset.TryParseExact(valueProviderResult.FirstValue, DateModelBinderDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"{valueProviderResult.FirstValue} is not a valid date string, it must be of the form {DateModelBinderDateFormat}");
                return Task.CompletedTask;
            }

            if (SurveyDependantModelBinderBase<object>.TryGetSubset(bindingContext, out var subset))
            {
                var profileResponseAccessor = bindingContext.HttpContext.GetService<IProfileResponseAccessorFactory>()
                    .GetOrCreate(subset);
                var endDate = profileResponseAccessor.EndDate;
                var startDate = profileResponseAccessor.StartDate;
                var resultDateOffsetNoOffset = result.ToDateInstance(); //This eliminates the offset

                if (resultDateOffsetNoOffset <= endDate && resultDateOffsetNoOffset >= startDate)
                {
                    bindingContext.Result = ModelBindingResult.Success(resultDateOffsetNoOffset);
                }
                else
                {
                    bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                        RequestedDateOutOfDateRange(valueProviderResult.FirstValue, startDate, endDate));
                }
            }

            return Task.CompletedTask;
        }

        public class Provider : IModelBinderProvider
        {
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                var modelType = context.Metadata.UnderlyingOrModelType;
                if (modelType == typeof(DateTime) || modelType == typeof(DateTimeOffset))
                {
                    return new DateModelBinder();
                }

                return null;
            }
        }
    }
}