#nullable enable
using System.Text;

namespace WebFormsCore.Security;

public class Csp
{
    private CspDirectiveGenerated? _scriptSrc;
    private CspDirectiveGenerated? _styleSrc;
    private CspDirective? _imgSrc;
    private CspDirective? _connectSrc;
    private CspDirective? _fontSrc;
    private CspDirective? _objectSrc;
    private CspDirective? _mediaSrc;
    private CspDirective? _frameSrc;
    private CspDirective? _sandbox;
    private CspDirective? _reportUri;
    private CspDirective? _childSrc;
    private CspDirective? _formAction;
    private CspDirective? _frameAncestors;
    private CspDirective? _pluginTypes;
    private CspDirective _baseUri;
    private CspDirective? _reportTo;
    private CspDirective? _workerSrc;
    private CspDirective? _manifestSrc;
    private CspDirective? _prefetchSrc;
    private CspDirective? _navigateTo;
    private CspDirective? _requireTrustedTypesFor;

    public Csp()
    {
        DefaultSrc = new CspDirective(this, "default-src", "'self'");
        _baseUri = new CspDirective(this, "base-uri", "'self'");
    }

    public bool Enabled { get; set; }

    public CspMode DefaultMode { get; set; } = CspMode.Nonce | CspMode.Uri;

    public CspDirective DefaultSrc { get; }

    public CspDirectiveGenerated ScriptSrc => _scriptSrc ??= new CspDirectiveGenerated(this, "script-src") { AllowDefault = false };

    public CspDirectiveGenerated StyleSrc => _styleSrc ??= new CspDirectiveGenerated(this, "style-src") { AllowDefault = false };

    public CspDirective ImgSrc => _imgSrc ??= new CspDirective(this, "img-src");

    public CspDirective ConnectSrc => _connectSrc ??= new CspDirective(this, "connect-src");

    public CspDirective FontSrc => _fontSrc ??= new CspDirective(this, "font-src");

    public CspDirective ObjectSrc => _objectSrc ??= new CspDirective(this, "object-src");

    public CspDirective MediaSrc => _mediaSrc ??= new CspDirective(this, "media-src");

    public CspDirective FrameSrc => _frameSrc ??= new CspDirective(this, "frame-src");

    public CspDirective Sandbox => _sandbox ??= new CspDirective(this, "sandbox");

    public CspDirective ReportUri => _reportUri ??= new CspDirective(this, "report-uri");

    public CspDirective ChildSrc => _childSrc ??= new CspDirective(this, "child-src");

    public CspDirective FormAction => _formAction ??= new CspDirective(this, "form-action");

    public CspDirective FrameAncestors => _frameAncestors ??= new CspDirective(this, "frame-ancestors");

    public CspDirective PluginTypes => _pluginTypes ??= new CspDirective(this, "plugin-types");

    public CspDirective BaseUri => _baseUri;

    public CspDirective ReportTo => _reportTo ??= new CspDirective(this, "report-to");

    public CspDirective WorkerSrc => _workerSrc ??= new CspDirective(this, "worker-src");

    public CspDirective ManifestSrc => _manifestSrc ??= new CspDirective(this, "manifest-src");

    public CspDirective PrefetchSrc => _prefetchSrc ??= new CspDirective(this, "prefetch-src");

    public CspDirective NavigateTo => _navigateTo ??= new CspDirective(this, "navigate-to");

    public CspDirective RequireTrustedTypesFor => _requireTrustedTypesFor ??= new CspDirective(this, "require-trusted-types-for") { AllowDefault = false };

    public void Write(StringBuilder builder)
    {
        DefaultSrc.Write(builder);
        _scriptSrc?.Write(builder);
        _styleSrc?.Write(builder);
        _imgSrc?.Write(builder);
        _connectSrc?.Write(builder);
        _fontSrc?.Write(builder);
        _objectSrc?.Write(builder);
        _mediaSrc?.Write(builder);
        _frameSrc?.Write(builder);
        _sandbox?.Write(builder);
        _reportUri?.Write(builder);
        _childSrc?.Write(builder);
        _formAction?.Write(builder);
        _frameAncestors?.Write(builder);
        _pluginTypes?.Write(builder);
        _baseUri.Write(builder);
        _reportTo?.Write(builder);
        _workerSrc?.Write(builder);
        _manifestSrc?.Write(builder);
        _prefetchSrc?.Write(builder);
        _navigateTo?.Write(builder);
        _requireTrustedTypesFor?.Write(builder);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        Write(sb);
        return sb.ToString();
    }
}
