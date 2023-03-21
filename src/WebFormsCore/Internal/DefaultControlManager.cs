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
        _types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetCustomAttributes<AssemblyViewAttribute>())
            .ToDictionary(x => NormalizePath(x.Path), x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<Type> Types => _types.Values;

    public Type GetType(string path)
    {
        path = NormalizePath(path);

        if (!_types.TryGetValue(path, out var type))
        {
            throw new InvalidOperationException($"Could not find type for path '{path}'");
        }

        return type;
    }

    public ValueTask<Type> GetTypeAsync(string path)
    {
        return new ValueTask<Type>(GetType(path));
    }

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        if (_types.ContainsKey(fullPath))
        {
            path = fullPath;
            return true;
        }

        if (_contentRoot is null || !fullPath.StartsWith(_contentRoot))
        {
            path = null;
            return false;
        }

        path = fullPath.Substring(_contentRoot.Length).TrimStart('\\', '/');
        return true;
    }

    private static string NormalizePath(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        var otherSeparator = separator == '/' ? '\\' : '/';

        return path.IndexOf(otherSeparator) == -1
            ? path
            : path.Replace(otherSeparator, separator);
    }

}
