using System;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Provides data for the <see cref="E:System.Web.UI.WebControls.Repeater.ItemCreated" /> and <see cref="E:System.Web.UI.WebControls.Repeater.ItemDataBound" /> events of a <see cref="T:System.Web.UI.WebControls.Repeater" />.
/// </summary>
public class RepeaterItemEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.RepeaterItemEventArgs" /> class.
    /// </summary>
    /// <param name="item">
    /// The <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event. The <see cref="P:System.Web.UI.WebControls.RepeaterItemEventArgs.Item" /> property is set to this value.
    /// </param>
    public RepeaterItemEventArgs(RepeaterItem item)
    {
        Item = item;
    }

    /// <summary>
    /// Gets the <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event.
    /// </summary>
    /// <returns>
    /// The <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event.
    /// </returns>
    public RepeaterItem Item { get; }
}

/// <summary>
/// Provides data for the <see cref="E:System.Web.UI.WebControls.Repeater.ItemCreated" /> and <see cref="E:System.Web.UI.WebControls.Repeater.ItemDataBound" /> events of a <see cref="T:System.Web.UI.WebControls.Repeater" />.
/// </summary>
public class RepeaterItemEventArgs<T> : RepeaterItemEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.RepeaterItemEventArgs" /> class.
    /// </summary>
    /// <param name="item">
    /// The <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event. The <see cref="P:System.Web.UI.WebControls.RepeaterItemEventArgs.Item" /> property is set to this value.
    /// </param>
    public RepeaterItemEventArgs(RepeaterItem<T> item)
        : base(item)
    {
        Item = item;
    }

    /// <summary>
    /// Gets the <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event.
    /// </summary>
    /// <returns>
    /// The <see cref="T:System.Web.UI.WebControls.RepeaterItem" /> associated with the event.
    /// </returns>
    public new RepeaterItem<T> Item { get; }
}
