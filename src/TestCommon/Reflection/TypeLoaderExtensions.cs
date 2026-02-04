using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestCommon.Reflection;

public static class TypeLoaderExtensions
{
    private static readonly IEnumerable<Type> AllTypes = typeof(TypeLoaderExtensions).Assembly.GetAllTypes();

    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException("assembly");
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    public static IEnumerable<Type> GetTypesWithInterface(this Type type)
    {
        return AllTypes.Where(type.IsAssignableFrom).ToList();
    }

    public static bool DescendantPropertyMentionsString(this Type type, HashSet<Type> visitedTypes,
        string lookingFor, string currentPath, out string path)
    {
        path = null;

        if (!visitedTypes.Add(type)) return false;

        foreach (var property in type.GetProperties())
        {
            var propertyPath = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";

            if (property.Name.Contains(lookingFor, StringComparison.OrdinalIgnoreCase))
            {
                path = propertyPath;
                return true;
            }

            if (DescendantPropertyMentionsString(property.PropertyType, visitedTypes, lookingFor,
                    propertyPath, out path))
            {
                return true;
            }
        }

        return false;
    }
    public static TAttribute[] GetMethodOrTypeAttributes<TAttribute>(this MemberInfo method) where TAttribute : Attribute
    {
        var attributes = method.GetCustomAttributes<TAttribute>(true).ToArray();
        if (attributes is []) attributes = method.DeclaringType?.GetCustomAttributes<TAttribute>(true).ToArray() ?? [];
        return attributes;
    }
}