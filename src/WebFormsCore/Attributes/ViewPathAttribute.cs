using System;

namespace WebFormsCore;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ViewPathAttribute : Attribute
{
    public ViewPathAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
