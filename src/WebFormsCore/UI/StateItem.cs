namespace WebFormsCore.UI;

public sealed class StateItem
{
    internal StateItem(object? initialValue, bool storeInView = false)
    {
        Value = initialValue;
        StoreInView = storeInView;
    }

    /// <summary>Gets or sets a value indicating whether the <see cref="T:WebFormsCore.UI.StateItem" /> object has been modified.</summary>
    /// <returns>
    /// <see langword="true" /> if the stored <see cref="T:WebFormsCore.UI.StateItem" /> object has been modified; otherwise, <see langword="false" />.</returns>
    public bool StoreInView { get; set; }

    /// <summary>Gets or sets the value of the <see cref="T:WebFormsCore.UI.StateItem" /> object that is stored in the <see cref="T:WebFormsCore.UI.StateBag" /> object.</summary>
    /// <returns>The value of the <see cref="T:WebFormsCore.UI.StateItem" /> stored in the <see cref="T:WebFormsCore.UI.StateBag" />.</returns>
    public object? Value { get; set; }
}
