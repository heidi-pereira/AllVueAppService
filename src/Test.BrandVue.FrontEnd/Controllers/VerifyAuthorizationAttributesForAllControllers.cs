using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using BrandVue.Controllers.Api;
using BrandVue.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Test.BrandVue.FrontEnd.Controllers
{
    [TestFixture]
    internal class VerifyAuthorizationAttributesForAllControllers
    {
        [Test]
        public void NoCallableMethodHasBothRoleAuthorisationAndAuthorizePolicy()
        {
            var controllerAssembly = typeof(ApiController).Assembly;
            var controllerTypes = controllerAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

            var failures = controllerTypes
                .SelectMany(controller =>
                    controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Select(method => new
                        {
                            Controller = controller,
                            Method = method,
                            HasRoleAuthorisation = method.GetCustomAttributes(typeof(RoleAuthorisationAttribute), true).Any(),
                            AuthorizePolicies = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                                .Cast<AuthorizeAttribute>()
                                .Where(a => !string.IsNullOrWhiteSpace(a.Policy))
                                .Select(a => a.Policy)
                                .ToList()
                        })
                        .Where(x => x.HasRoleAuthorisation && x.AuthorizePolicies.Any())
                )
                .ToList();

            if (failures.Any())
            {
                var message = string.Join(Environment.NewLine, failures.Select(f =>
                    $"Controller: {f.Controller.Name}, Method: {f.Method.Name}, Policies: {string.Join(", ", f.AuthorizePolicies)}"));
                Assert.Fail("Some controller methods have both [RoleAuthorisation] and [Authorize(Policy = ...)]:" + Environment.NewLine + message);
            }
        }
    }
}
