namespace WebFormsCore.UI.Attributes;

public class BoolAttributeParser : IAttributeParser<bool>
{
    public bool Parse(string value)
    {
        return value switch
        {
            "true" or "True" => true,
            "false" or "False" => false,
            _ => bool.Parse(value)
        };
    }
}