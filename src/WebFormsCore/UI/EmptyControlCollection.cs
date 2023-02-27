using System;

namespace WebFormsCore.UI;

public class EmptyControlCollection : ControlCollection
{
    public EmptyControlCollection(Control owner) : base(owner)
    {
    }

    public override void AddWithoutPageEvents(Control child)
    {
        throw new NotSupportedException();
    }
}
