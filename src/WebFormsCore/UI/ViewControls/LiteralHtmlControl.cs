﻿using System;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public class LiteralHtmlControl : HtmlGenericControl
{
    protected override bool ProcessControl => false;

    protected override bool ProcessChildren => HasControls();

    protected override bool GenerateAutomaticID => false;

    public LiteralHtmlControl()
    {
    }

    public LiteralHtmlControl(string tag) : base(tag)
    {
    }

    protected override bool? EnableViewStateSelf
    {
        get => false;
        set
        {
            if (value == true)
            {
                throw new InvalidOperationException("Cannot set EnableViewState to true for a LiteralHtmlControl.");
            }
        }
    }

    protected override ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        return Attributes.RenderAsync(writer);
    }

    protected override void TrackViewState(ViewStateProvider provider)
    {
    }

    protected override void OnWriteViewState(ref ViewStateWriter writer)
    {
    }

    protected override void OnLoadViewState(ref ViewStateReader reader)
    {
    }
}
