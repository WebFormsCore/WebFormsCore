namespace WebFormsCore.Middlewares;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PageAttribute : Attribute
{
    public string Path { get; set; }
}
