using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Dashboard;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrandVue.Filters;

public class NbspFilterAttribute : ActionFilterAttribute
{
    private string _nbspUnicodeString = char.Parse("\u00a0").ToString();
    private const string nbspHtmlString = "&nbsp;";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Method == "GET")
        {
            return;
        }

        foreach (var argument in context.ActionArguments)
        {
            switch (argument)
            {
                case { Key: "page", Value: PageDescriptor page }:
                    foreach (var pane in page.Panes)
                    {
                        foreach (var part in pane.Parts)
                        {
                            if (!string.IsNullOrWhiteSpace(part.Spec1))
                            {
                                if (part.Spec1.Contains(nbspHtmlString) ||
                                    part.Spec1.Contains(_nbspUnicodeString))
                                {
                                    part.Spec1 = part.Spec1.Replace(nbspHtmlString, " ");
                                    part.Spec1 = part.Spec1.Replace(_nbspUnicodeString, " ");
                                }
                            }
                        }
                    }

                    break;

                case { Key: "metricConfiguration", Value: MetricConfiguration metricConfiguration }:
                    if (metricConfiguration.Name.Contains(nbspHtmlString) ||
                        metricConfiguration.Name.Contains(_nbspUnicodeString))
                    {
                        metricConfiguration.Name = metricConfiguration.Name.Replace(nbspHtmlString, " ");
                        metricConfiguration.Name = metricConfiguration.Name.Replace(_nbspUnicodeString, " ");
                    }
                    
                    break;
            }
        }
    }
}