using System;
using System.Globalization;
using WebFormsCore.Security;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int PostbackCount { get; set; }

    protected override void OnInit(EventArgs args)
    {
        Csp.Enabled = true;
        Csp.ScriptSrc.Mode = CspMode.Sha256;
        Csp.FormAction.SourceList.Add("'none'");
        Csp.StyleSrc.SourceList.Add("https://cdn.jsdelivr.net");
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }
}
