namespace System.Web.UI;

public class EmptyControlCollection : ControlCollection
{
    public EmptyControlCollection(Control owner) : base(owner)
    {
    }

    public override void Add(Control child)
    {
        throw new NotSupportedException();
    }

    public override void AddAt(int index, Control child)
    {
        throw new NotSupportedException();
    }
}
