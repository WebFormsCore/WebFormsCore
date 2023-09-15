using System;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Class)]
public class CompiledViewAttribute : Attribute
{
    public CompiledViewAttribute(string path, string hash)
    {
        Path = path;
        Hash = hash;
    }

    public string Path { get; }

    public string Hash { get; }
}