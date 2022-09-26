using System;
using System.Globalization;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    protected override void OnInit(EventArgs args)
    {
        title.InnerText = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    }
}
