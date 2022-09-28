using System;
using System.Globalization;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int PostbackCount { get; set; }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }
}
