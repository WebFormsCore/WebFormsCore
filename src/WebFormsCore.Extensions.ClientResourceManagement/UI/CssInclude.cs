namespace WebFormsCore.UI;

public class CssInclude : ClientDependencyInclude
{
    private CssMediaType _cssMedia;

    public CssInclude()
    {
        DependencyType = ClientDependencyType.Css;
    }

    public CssInclude(IClientDependencyFile file)
        : base(file)
    {
        DependencyType = ClientDependencyType.Css;
    }

    public CssMediaType CssMedia
    {
        get => _cssMedia;
        set
        {
            if (value != CssMediaType.All)
            {
                Attributes["media"] = value switch
                {
                    CssMediaType.Screen => "screen",
                    CssMediaType.Print => "print",
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
            }
            else
            {
                Attributes.Remove("media");
            }

            _cssMedia = value;
        }
    }
}
