using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IControlManager
{
    Type GetType(string path);

    ValueTask<Type> GetTypeAsync(string path);

    bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path);
}