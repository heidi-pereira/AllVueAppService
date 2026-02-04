using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc;
using TestCommon.Reflection;
using VerifyNUnit;
using Vue.Common.Constants.Constants;

namespace Test.BrandVue.FrontEnd;

class SubsetAuthorisationAttributeTests
{
    // If you accept an exception to this rule, please write a note in the method's xml doc explaining why it's special
    [Test]
    public async Task ModelsMentioningSubsetShouldUseSubsetAuthorization()
    {
        var controllerTypes = GetControllerTypes();
        var methodsMissingISubsetIdProvider = GetMethodsWithSubsetAuthIssues(controllerTypes)
            .OrderBy(x => x.Issue).ThenBy(x => x.Method).ToArray();

        await Verifier.Verify(string.Join("\r\n", methodsMissingISubsetIdProvider.Select(m => $"{m.Method}: Path: {m.Path}, Issue {m.Issue}")));
    }

    private static List<(string Method, string Path, bool? IsValidSubsetAuth, string Issue, bool SysAdminOnly)> GetMethodsWithSubsetAuthIssues(IEnumerable<Type> controllerTypes)
    {
        return controllerTypes
            .SelectMany(controllerType => controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                (_, method) => AnalyseControllerMethod(method))
            .Where(result => result is { SysAdminOnly: false, Path: not null } && result.IsValidSubsetAuth != true || result.IsValidSubsetAuth == false)
            .ToList();
    }

    private static (string Method, string Path, bool? IsValidSubsetAuth, string Issue, bool SysAdminOnly) AnalyseControllerMethod(MethodInfo method)
    {
        var pathToSubset = MethodMentionsSubset(method, out var path) ? path : null;
        bool? isValidSubsetAuth = IsValidSubsetAuth(method, out var issue);

        var roleAuthAttributes = method.GetMethodOrTypeAttributes<RoleAuthorisationAttribute>();
        var sysAdminOnly = roleAuthAttributes.All(a => a.Roles is [Roles.SystemAdministrator]);
        return (Method: $"{method.DeclaringType?.Name}.{method.Name}", Path: pathToSubset, IsValidSubsetAuth: isValidSubsetAuth, Issue: issue, SysAdminOnly: sysAdminOnly);
    }

    private static bool MethodHasISubsetIdProvider(MethodInfo method)
    {
        return method.GetParameters()
            .Any(parameter => typeof(ISubsetIdProvider).IsAssignableFrom(parameter.ParameterType) ||
                              typeof(ISubsetIdsProvider<IEnumerable<string>>).IsAssignableFrom(parameter.ParameterType));
    }

    private static bool MethodMentionsSubset(MethodInfo method, out string path)
    {
        path = string.Empty;
        foreach (var parameter in method.GetParameters())
        {
            if (ParameterMentionsSubset(parameter, out var parameterPath))
            {
                path = parameterPath;
                return true;
            }
        }
        return false;
    }

    private static bool ParameterMentionsSubset(ParameterInfo parameter, out string path)
    {
        path = parameter.Name;
        if (parameter.Name?.Contains("subset", StringComparison.OrdinalIgnoreCase) == true) return true;
        if (parameter.ParameterType.DescendantPropertyMentionsString(new HashSet<Type>(), "subset", path, out path))
        {
            return true;
        }
        return false;
    }

    private static bool? IsValidSubsetAuth(MethodInfo method, out string issue)
    {
        issue = null;
        if (HasSubsetAuthorisationAttribute(method, out var stringParameter))
        {
            if (stringParameter is not null)
            {
                var actualParameterNames = method.GetParameters().Select(x => x.Name).ToList();
                if (actualParameterNames.Contains(stringParameter)) return true;
                issue = $"Parameters ({string.Join(", ", actualParameterNames)}) must contain {stringParameter}";
                return false;
            }
            if (MethodHasISubsetIdProvider(method)) return true;

            issue = "No parameter implements ISubsetIdProvider";
            return false;
        }

        issue = $"Missing {nameof(SubsetAuthorisationAttribute)} from method";
        return null;
    }

    private static bool HasSubsetAuthorisationAttribute(MemberInfo method, out string optionalStringParameterName)
    {
        var attributes = method.GetMethodOrTypeAttributes<SubsetAuthorisationAttribute>();
        if (attributes.Length > 1)
        {
            throw new InvalidDataException(
                $"Multiple SubsetAuthorisationAttribute on {method.DeclaringType?.Name}.{method.Name}");
        }

        optionalStringParameterName = attributes.SingleOrDefault()?.StringSubsetIdParameterName;
        return attributes is not [];
    }
    private static IEnumerable<Type> GetControllerTypes()
    {
        return typeof(global::BrandVue.Controllers.HomeController).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)));
    }
}
