namespace WebFormsCore.UI.Attributes;

public interface IAttributeParser<out T>
{
    T Parse(string value);
}
