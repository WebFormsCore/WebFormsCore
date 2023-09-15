using System;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CompiledViewInclude : Attribute
{
    public CompiledViewInclude(string path, string hash)
    {
        Path = path;
        Hash = hash;
    }

    public string Path { get; }

    public string Hash { get; }
}
