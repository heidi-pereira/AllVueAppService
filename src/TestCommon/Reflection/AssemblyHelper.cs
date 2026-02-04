using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestCommon.Reflection
{
    public static class AssemblyHelper
    {
        public static IEnumerable<Type> GetAllTypes(this Assembly assembly)
        {
            var dependencies = new HashSet<Assembly>();
            GetDependentAssembliesRecursive(assembly, dependencies);
            return dependencies.SelectMany(d => TypeLoaderExtensions.GetLoadableTypes(d));
        }

        private static void GetDependentAssembliesRecursive(Assembly assembly, HashSet<Assembly> dependencies)
        {
            if (!dependencies.Add(assembly)) return;

            var referencedAssemblies = assembly.GetReferencedAssemblies();

            foreach (var referencedAssembly in referencedAssemblies)
            {
                if(!referencedAssembly.FullName.StartsWith("BrandVue.")) continue;

                try
                {
                    var loadedAssembly = Assembly.Load(referencedAssembly);
                    GetDependentAssembliesRecursive(loadedAssembly, dependencies);
                }
                catch (Exception)
                {
                    // Ignore any exceptions, as some referenced assemblies may not be available
                }
            }
        }
    }
}