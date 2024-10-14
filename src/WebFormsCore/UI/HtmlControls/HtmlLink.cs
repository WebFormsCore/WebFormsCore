﻿using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlLink : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlLink()
        : base("link")
    {
    }

    public override ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!HtmlStyle.RenderStyles(this) && Attributes["rel"] == "stylesheet")
        {
            return default;
        }

        return base.RenderAsync(writer, token);
    }
}
