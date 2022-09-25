namespace WebFormsCore.UI.Attributes;

public class StringAttributeParser : IAttributeParser<string>
{
    public string Parse(string value)
    {
        return value;
    }
}