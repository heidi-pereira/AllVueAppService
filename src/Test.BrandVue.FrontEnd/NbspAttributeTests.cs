using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.SourceData.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class NbspAttributeTests
    {
        [TestCase("Test&nbsp;Test", Description = "Explicit nbsp;")]
        [TestCase("Test\u00a0Test", Description = "Unicode nbsp;")]
        public void PageDescriptorSpec1ContainsNbsp(string spec1)
        {
            var pageDescriptor = new PageDescriptor
            {
                Panes = new[]
                {
                    new PaneDescriptor
                    {
                        Parts = new[]
                        {
                            new PartDescriptor
                            {
                                Spec1 = spec1
                            }
                        }
                    }
                }
            };

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            Dictionary<string, object?> actionArguments = new Dictionary<string, object?>
            {
                {"page", pageDescriptor}
            };

            var nbspFilterAttribute = new NbspFilterAttribute();
            var actionExecutingContext = new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>(),
                actionArguments, null);

            nbspFilterAttribute.OnActionExecuting(actionExecutingContext);

            Assert.That(pageDescriptor.Panes.First().Parts.First().Spec1, Is.EqualTo("Test Test"));
        }

        [TestCase("Test&nbsp;Test", Description = "Explicit nbsp;")]
        [TestCase("Test\u00a0Test", Description = "Unicode nbsp;")]
        public void MetricNameContainsNbsp(string metricName)
        {
            var metricConfiguration = new MetricConfiguration
            {
                Name = metricName
            };

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            Dictionary<string, object?> actionArguments = new Dictionary<string, object?>
            {
                {"metricConfiguration", metricConfiguration}
            };

            var nbspFilterAttribute = new NbspFilterAttribute();
            var actionExecutingContext = new ActionExecutingContext(
                               new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>(),
                                              actionArguments, null);

            nbspFilterAttribute.OnActionExecuting(actionExecutingContext);

            Assert.That(metricConfiguration.Name, Is.EqualTo("Test Test"));
        }
    }

}