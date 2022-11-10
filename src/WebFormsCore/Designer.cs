// These interfaces are to trick the designer into thinking that controls are extending System.Web.UI.Control

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
namespace System.Web.UI;

internal interface Control
{
}

internal interface Page : Control
{
}
