using WebForms.Nodes;

namespace WebForms.Collections;

public class HitRangeComparer : IComparer<HitRange>, IEqualityComparer<HitRange>
{
    public static readonly HitRangeComparer Instance = new();

    public int Compare(HitRange? x, HitRange? y)
    {
        if (x == null) return -1;
        if (y == null) return 1;

        return x.Range.Start.Offset.CompareTo(y.Range.Start.Offset);
    }

    public bool Equals(HitRange? x, HitRange? y)
    {
        if (x == null) return y == null;

        return x.Equals(y);
    }

    public int GetHashCode(HitRange obj)
    {
        return obj.GetHashCode();
    }
}
