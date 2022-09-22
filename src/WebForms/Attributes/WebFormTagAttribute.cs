namespace System.Web;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class WebFormTagAttribute : Attribute
{
    public WebFormTagAttribute(string tagPrefix, string ns)
    {
        TagPrefix = tagPrefix;
        Namespace = ns;
    }

    public string TagPrefix { get; }

    public string Namespace { get; }
}
