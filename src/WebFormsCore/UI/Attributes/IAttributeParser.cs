namespace WebFormsCore.UI.Attributes;

public interface IAttributeParser
{
    bool SupportsRouteConstraint(string name);
}

public interface IAttributeParser<out T> : IAttributeParser
{
    T Parse(string value);
}
