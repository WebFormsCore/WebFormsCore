using WebForms.Models;

namespace WebForms.Collections;

public class ControlKeyCompare : IEqualityComparer<ControlKey>
{
    public static readonly ControlKeyCompare OrdinalIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

    private readonly StringComparer _comparer;

    public ControlKeyCompare(StringComparer comparer)
    {
        _comparer = comparer;
    }

    public bool Equals(ControlKey x, ControlKey y)
    {
        return _comparer.Equals(x.Namespace, y.Namespace) && _comparer.Equals(x.Name, y.Name);
    }

    public int GetHashCode(ControlKey obj)
    {
        checked
        {
            return (_comparer.GetHashCode(obj.Namespace) * 397) ^ _comparer.GetHashCode(obj.Name);
        }
    }
}
