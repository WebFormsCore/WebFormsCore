using WebForms.Models;

namespace WebForms.Collections;

public class AttributeCompare : IComparer<TokenString>, IEqualityComparer<TokenString>
{
    public static readonly AttributeCompare IgnoreCase = new(StringComparer.OrdinalIgnoreCase);

    private readonly StringComparer _comparer;

    public AttributeCompare(StringComparer comparer)
    {
        _comparer = comparer;
    }

    public int Compare(TokenString x, TokenString y)
    {
        return _comparer.Compare(x.Value, y.Value);
    }

    public bool Equals(TokenString x, TokenString y)
    {
        return _comparer.Equals(x.Value, y.Value);
    }

    public int GetHashCode(TokenString obj)
    {
        return _comparer.GetHashCode(obj.Value);
    }
}