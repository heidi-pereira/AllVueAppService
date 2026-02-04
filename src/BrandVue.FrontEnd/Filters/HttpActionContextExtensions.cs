using System.Threading;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrandVue.Filters
{
    internal static class HttpActionContextExtensions
    {
        public static string[] GetModelSubsetIds(this ActionExecutingContext actionContext, string subsetIdParameterName)
        {
            return TryGetSubsetIfForExplicitNamedParameter(actionContext, subsetIdParameterName, out string subsetId)
                ? [subsetId]
                : GetSubsetsFromParameters(actionContext);
        }

        private static bool TryGetSubsetIfForExplicitNamedParameter(ActionExecutingContext actionContext,
            string subsetIdParameterName, out string subsetId)
        {
            if (subsetIdParameterName != null)
            {
                if (actionContext.ActionArguments.TryGetValue(subsetIdParameterName, out var objModel) &&
                    objModel is string strModel)
                {
                    subsetId = strModel;
                    return true;
                }

                throw new ArgumentOutOfRangeException(nameof(subsetIdParameterName), subsetIdParameterName,
                    $"Stop passing the {nameof(subsetIdParameterName)} to {nameof(SubsetAuthorisationAttribute)} or ensure it references a string parameter that exists.");
            }

            subsetId = null;
            return false;
        }

        private static string[] GetSubsetsFromParameters(ActionExecutingContext actionContext)
        {
            var values = actionContext.ActionArguments.Select(x => x.Value).ToArray();
            return values.OfType<ISubsetIdProvider>().Select(s => s.SubsetId)
                    .Concat(values.OfType<ISubsetIdsProvider>().SelectMany(s => s.SubsetIds)).ToArray();
        }
    }
}