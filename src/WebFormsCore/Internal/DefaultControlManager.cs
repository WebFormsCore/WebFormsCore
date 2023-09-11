using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebFormsCore;

public class DefaultControlManager : IControlManager
{
    private readonly string? _contentRoot;
    private readonly Dictionary<string, Type> _types;

    public DefaultControlManager(IWebFormsEnvironment? environment = null)
    {
        _contentRoot = environment?.ContentRootPath ?? AppContext.BaseDirectory;
        _types = GetCompiledControls();
    }

    public IEnumerable<Type> Types => _types.Values;

    public Type GetType(string path)
    {
        path = NormalizePath(path);

        if (!_types.TryGetValue(path, out var type))
        {
            throw new FileNotFoundException($"Could not find type for path '{path}'");
        }

        return type;
    }

    public ValueTask<Type> GetTypeAsync(string path)
    {
        return new ValueTask<Type>(GetType(path));
    }

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        var current = fullPath;

        if (_contentRoot != null && current.StartsWith(_contentRoot))
        {
            current = current.Substring(_contentRoot.Length);
        }

        current = NormalizePath(current);

        if (!_types.ContainsKey(current))
        {
            path = null;
            return false;
        }

        path = current;
        return true;
    }

    public static Dictionary<string, Type> GetCompiledControls()
    {
        var types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var entry = Assembly.GetEntryAssembly();

        if (entry != null)
        {
            AddAssemblies(entry, types);
        }

        return types;

        static void AddAssemblies(Assembly current, IDictionary<string, Type> types)
        {
            foreach (var attribute in current.GetCustomAttributes<AssemblyViewAttribute>())
            {
                if (!types.ContainsKey(attribute.Path))
                {
                    types.Add(NormalizePath(attribute.Path), attribute.Type);
                }
            }

            foreach (var assembly in current.GetReferencedAssemblies())
            {
                try
                {
                    AddAssemblies(Assembly.Load(assembly), types);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var span = path.AsSpan();

#if NET
        var hasInvalidPathSeparator = span.Contains('\\');
#else
        var hasInvalidPathSeparator = span.IndexOf('\\') != -1;
#endif

        if (!hasInvalidPathSeparator && span[0] != '/')
        {
            return path;
        }

        Span<char> buffer = stackalloc char[path.Length];

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];

            buffer[i] = c switch
            {
                '\\' => '/',
                _ => c
            };
        }

        if (buffer[0] == '/')
        {
            return buffer.Slice(1).ToString();
        }

        return buffer.ToString();
    }
}
