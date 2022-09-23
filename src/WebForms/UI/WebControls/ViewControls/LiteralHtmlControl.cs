﻿using System;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public class LiteralHtmlControl : HtmlGenericControl
{
    public LiteralHtmlControl()
    {
    }

    public LiteralHtmlControl(string tag) : base(tag)
    {
    }

    public override bool EnableViewState
    {
        get => false;
        set
        {
            if (value)
            {
                throw new InvalidOperationException("Cannot set EnableViewState to true for a LiteralHtmlControl.");
            }
        }
    }

    protected override void OnWriteViewState(ref ViewStateWriter writer)
    {
    }

    protected override void OnReadViewState(ref ViewStateReader reader)
    {
    }
}