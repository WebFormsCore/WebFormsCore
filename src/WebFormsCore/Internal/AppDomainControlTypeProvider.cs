using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace WebFormsCore;

public class AppDomainControlTypeProvider : IControlTypeProvider
{
    public Dictionary<string, Type> GetTypes()
    {
        return GetAndWatchTypes();
    }

    private static Dictionary<string, Type> GetAndWatchTypes()
    {
        var types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        try
        {
            AddCompiledControls(types, Assembly.GetEntryAssembly());
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) => AddCompiledControls(types, args.LoadedAssembly);
        }
        catch (PlatformNotSupportedException)
        {
            // Assume AOT
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(assembly, types);
            }
        }

        return types;
    }

    private static void AddCompiledControls(Dictionary<string, Type> types, Assembly? entry)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(i => i.GetName().FullName);
        var visited = new HashSet<Assembly>();

        if (entry != null)
        {
            AddAssemblies(entry, types, loaded, visited);
        }

        return;
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "")]
        static void AddAssemblies(Assembly current, IDictionary<string, Type> types, IDictionary<string, Assembly> assemblies, HashSet<Assembly> visited)
        {
            if (!visited.Add(current))
            {
                return;
            }

            AddAssembly(current, types);

            foreach (var assemblyName in current.GetReferencedAssemblies())
            {
                if (assemblies.TryGetValue(assemblyName.FullName, out var assembly))
                {
                    AddAssemblies(assembly, types, assemblies, visited);
                }
            }
        }
    }

    private static void AddAssembly(Assembly assembly, IDictionary<string, Type> types)
    {
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyViewAttribute>())
        {
            if (!types.ContainsKey(attribute.Path))
            {
                types.Add(DefaultControlManager.NormalizePath(attribute.Path), attribute.Type);
            }
        }
    }
}
