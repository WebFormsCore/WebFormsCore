﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Security;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

[ParseChildren(false)]
public class Page : Control, INamingContainer, IStateContainer, System.Web.UI.Page, IInternalPage
{
    private HttpContext? _context;
    private ScopedControlContainer? _scopedContainer;

    internal List<object>? ChangedPostDataConsumers;
    private bool _validated;
    private List<IValidator>? _validators;

    public Page()
    {
        ClientScript = new ClientScriptManager(this);
    }

    public bool EnableCsp
    {
        get => Csp.Enabled;
        set => Csp.Enabled = value;
    }

    public HtmlHead? Header { get; internal set; }

    public HtmlBody? Body { get; internal set; }

    public Csp Csp { get; set; } = new();

    public ClientScriptManager ClientScript { get; }

    public StreamPanel? ActiveStreamPanel { get; set; }

    public override HttpContext Context => _context ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; internal set; }

    public bool IsExternal { get; internal set; }

    public bool IsStreaming => ActiveStreamPanel != null;

    protected override IServiceProvider ServiceProvider => Context.RequestServices;

    private ScopedControlContainer ScopedContainer => _scopedContainer ??= ServiceProvider.GetRequiredService<ScopedControlContainer>();

    public List<HtmlForm> Forms { get; set; } = new();

    public List<IValidator> Validators
    {
        get => _validators ??= new List<IValidator>();
        set => _validators = value;
    }

    public bool IsValid
    {
        get
        {
            if (!_validated)
            {
                throw new InvalidOperationException("Page has not been validated yet.");
            }

            if (_validators is null)
            {
                return true;
            }

            var isValid = true;

            foreach (var validator in Validators)
            {
                if (!validator.IsValid)
                {
                    isValid = false;
                }
            }

            return isValid;
        }
    }

    public override HtmlForm? Form => null;

    internal HtmlForm? ActiveForm { get; set; }

    public async ValueTask RaiseChangedEventsAsync(CancellationToken cancellationToken)
    {
        if (ChangedPostDataConsumers is not { } consumers)
        {
            return;
        }

        foreach (var consumer in consumers)
        {
            if (consumer is IPostBackAsyncDataHandler eventHandler)
            {
                await eventHandler.RaisePostDataChangedEventAsync(cancellationToken);
            }
            else if (consumer is IPostBackDataHandler handler)
            {
                handler.RaisePostDataChangedEvent();
            }
        }
    }

    internal void ClearChangedPostDataConsumers()
    {
        ChangedPostDataConsumers?.Clear();
    }

    protected internal virtual void SetContext(HttpContext context)
    {
        _context = context;
        IsPostBack = string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    void IInternalPage.SetContext(HttpContext context) => SetContext(context);

    protected internal virtual void RegisterDisposable(Control control)
    {
        ScopedContainer.Register(control);
    }

    protected override string GetUniqueIDPrefix() => "p$";

    public void SetValidatorInvalidControlFocus(string? validateId)
    {
        // TODO: implement
    }

    public async ValueTask<bool> ValidateAsync(string? validationGroup = null)
    {
        _validated = true;

        if (_validators is null)
        {
            return true;
        }

        var isValid = true;

        foreach (var validator in Validators)
        {
            if (validationGroup is not null &&
                (validator is not BaseValidator v || !string.Equals(v.ValidationGroup, validationGroup, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await validator.ValidateAsync();

            if (!validator.IsValid)
            {
                isValid = false;
            }
        }

        return isValid;
    }

    public string GetDefaultValidationGroup(Control control)
    {
        return Page.Validators
            .OfType<BaseValidator>()
            .FirstOrDefault(v => v.GetControlToValidate() == control)
            ?.ValidationGroup ?? "";
    }
}
