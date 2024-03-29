﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Events;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlHead() : HtmlContainerControl("head")
{
    protected override bool GenerateAutomaticID => false;

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        Page.Header ??= this;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value;

        if (options?.AddWebFormsCoreHeadScript ?? true)
        {
            Page.ClientScript.RegisterStartupScript(
                typeof(Page),
                "FormPostback",
                $$$"""window.wfc={hiddenClass:'{{{options?.HiddenClass ?? ""}}}',_:[],bind:function(a,b){this._.push([0,a,b])},bindValidator:function(a,b){this._.push([1,a,b])},init:function(a){this._.push([2,'',a])}};""",
                position: ScriptPosition.HeadStart);
        }

        if (options?.EnableWebFormsPolyfill ?? true)
        {
            Page.ClientScript.RegisterStartupStaticScript(
                typeof(Page),
                "/js/webforms-polyfill.min.js",
                Resources.Polyfill,
                position: ScriptPosition.HeadStart);
        }
    }

    protected override void OnUnload(EventArgs args)
    {
        base.OnUnload(args);

        if (Page.Header == this)
        {
            Page.Header = null;
        }
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderHeadStart(writer);
        }

        await base.RenderChildrenAsync(writer, token);

        foreach (var renderer in Context.RequestServices.GetServices<IPageService>())
        {
            await renderer.RenderHeadAsync(Page, writer, token);
        }

        if (!Page.IsPostBack)
        {
            await Page.ClientScript.RenderHeadEnd(writer);
        }
    }
}
