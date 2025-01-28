using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Security;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

[ParseChildren(false)]
public class Page : Control, INamingContainer, IStateContainer, IInternalPage
#pragma warning disable CS0436 // Type conflicts with imported type
    , System.Web.UI.Page
#pragma warning restore CS0436
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

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        RegisterScript();
    }

    protected override void OnPreRender(EventArgs args)
    {
        base.OnPreRender(args);

        ClientScript.OnPreRender();
    }

    private void RegisterScript()
    {
        // TODO: Move this to a better place.
        var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value;

        if (options?.AddWebFormsCoreScript ?? true)
        {
            Page.ClientScript.RegisterStartupDeferStaticScript(
                typeof(Page),
                "/js/form.min.js",
                Resources.Script);
        }

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
            Page.ClientScript.RegisterStartupScript(
                typeof(Page),
                "WebFormsCorePolyfill",
                Resources.Polyfill,
                position: ScriptPosition.HeadStart);
        }
    }

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

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (Header is null)
        {
            await HtmlHead.RenderHeadStartAsync(this, writer);
            await HtmlHead.RenderHeadEndAsync(this, writer, token);
        }

        if (Body is null)
        {
            await HtmlBody.RenderBodyStartAsync(this, writer);
            await base.RenderChildrenAsync(writer, token);
            await HtmlBody.RenderBodyEndAsync(this, writer, token);
        }
        else
        {
            await base.RenderChildrenAsync(writer, token);
        }
    }
}
