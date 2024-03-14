using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore;

internal class StaticFiles
{
    public static ConcurrentDictionary<PathString, string> Files { get; } = new();
}
