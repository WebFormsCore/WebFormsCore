using System;

namespace WebFormsCore;

public class ViewPathAttribute : Attribute
{
    public ViewPathAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
