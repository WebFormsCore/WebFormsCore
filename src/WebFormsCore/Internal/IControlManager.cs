using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IControlManager
{
    IReadOnlyDictionary<string, Type> ViewTypes { get; }

    Type GetType(string path);

    ValueTask<Type> GetTypeAsync(string path);

    bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path);
}