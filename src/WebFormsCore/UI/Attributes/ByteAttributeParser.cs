namespace WebFormsCore.UI.Attributes;

public class ByteAttributeParser : IAttributeParser<byte>
{
    public bool SupportsRouteConstraint(string name) => false;

    public byte Parse(string value)
    {
        return byte.Parse(value);
    }
}
