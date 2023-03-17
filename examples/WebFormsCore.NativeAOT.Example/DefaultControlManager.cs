using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebFormsCore.NativeAOT.Example;

public class DefaultControlManager : IControlManager
{
    private readonly Dictionary<string, Type> _types;

    public DefaultControlManager()
    {
        _types = typeof(DefaultControlManager).Assembly
            .GetCustomAttributes<AssemblyViewAttribute>()
            .ToDictionary(x => x.Path, x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<Type> Types => _types.Values;

    public Type GetType(string path)
    {
        if (!_types.TryGetValue(path, out var type))
        {
            throw new InvalidOperationException($"Could not find type for path '{path}'");
        }

        return type;
    }

    public ValueTask<Type> GetTypeAsync(string path)
    {
        return ValueTask.FromResult(GetType(path));
    }

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        path = fullPath;
        return true;
    }
}
