﻿using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlScript : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlScript()
        : base("script")
    {
    }

    protected override Task OnPreRenderAsync(CancellationToken token)
    {
        if (Uri.TryCreate(Attributes["src"], UriKind.Absolute, out var href))
        {
            Page.Csp.ScriptSrc.SourceList.Add($"{href.Scheme}://{href.Host}");
        }

        return Task.CompletedTask;
    }
}
