namespace System.Web.UI;

public interface IAttributeAccessor
{
    string? GetAttribute(string key);

    void SetAttribute(string key, string value);
}
