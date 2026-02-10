using WebFormsCore.UI;

namespace WebFormsCore.Tests;

/// <summary>
/// Simple <see cref="ITemplate"/> implementation that invokes a delegate to populate a container.
/// Used for creating templates imperatively in tests.
/// </summary>
internal sealed class InlineTemplate(Action<Control> instantiate) : ITemplate
{
    public void InstantiateIn(Control container)
    {
        instantiate(container);
    }
}
