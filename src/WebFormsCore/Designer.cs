// These interfaces are to trick the designer into thinking that the System.Web.UI.Control are implemented.

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
namespace System.Web.UI;

internal interface Control
{
}

internal interface Page : Control
{
}
