using System;
using System.Globalization;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] private int _postbackCount;

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (_postbackCount++).ToString();
    }
}
