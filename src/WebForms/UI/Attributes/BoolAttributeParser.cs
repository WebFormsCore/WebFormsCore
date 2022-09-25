namespace WebFormsCore.UI.Attributes;

public class BoolAttributeParser : IAttributeParser<bool>
{
    public bool Parse(string value)
    {
        return bool.Parse(value);
    }
}