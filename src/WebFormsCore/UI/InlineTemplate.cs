using System;

namespace WebFormsCore.UI;

public static class InlineTemplateMethods
{
    public static InlineTemplate Template(Func<Control[]> instantiate)
    {
        return new InlineTemplate(container =>
        {
            foreach (var control in instantiate())
            {
                container.Controls.AddWithoutPageEvents(control);
            }
        });
    }

    public static InlineTemplate Template(Func<Control, Control[]> instantiate)
    {
        return new InlineTemplate(container =>
        {
            foreach (var control in instantiate(container))
            {
                container.Controls.AddWithoutPageEvents(control);
            }
        });
    }
}

/// <summary>
/// Simple <see cref="ITemplate"/> implementation that invokes a delegate to populate a container.
/// Used for creating templates imperatively in tests.
/// </summary>
public sealed class InlineTemplate(Action<Control> instantiate) : ITemplate
{
    public void InstantiateIn(Control container)
    {
        instantiate(container);
    }
}
