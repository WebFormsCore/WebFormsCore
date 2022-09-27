// These interfaces are to trick the designer into thinking that the System.Web.UI.Control are implemented.
// They server no other purpose.

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
namespace System.Web.UI;

internal interface Control
{
}

internal interface Page : Control
{
}
