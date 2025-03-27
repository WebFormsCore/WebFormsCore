using System;
using System.Diagnostics.CodeAnalysis;
using WebFormsCore.UI;

namespace WebFormsCore.Containers;

public interface IControlAccessor
{
    Control Control { get; }
}

internal class ControlAccessor : IControlAccessor
{
    [field: AllowNull, MaybeNull]
    public Control Control
    {
        get => field ?? throw new InvalidOperationException("No control has been set");
        set;
    }
}
