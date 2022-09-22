using System;

namespace WebFormsCore;

public class CompiledViewAttribute : Attribute
{
    public CompiledViewAttribute(string hash)
    {
        Hash = hash;
    }

    public string Hash { get; }
}
