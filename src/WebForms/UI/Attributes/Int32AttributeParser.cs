namespace WebFormsCore.UI.Attributes;

public class Int32AttributeParser : IAttributeParser<int>
{
    public int Parse(string value)
    {
        return int.Parse(value);
    }
}