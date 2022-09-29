#nullable enable
using System.Text;

namespace WebFormsCore.Security;

public class Csp
{
    private CspDirective? _scriptSrc;
    private CspDirective? _styleSrc;
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
    private CspDirective? _baseUri;
    private CspDirective? _reportTo;
    private CspDirective? _workerSrc;
    private CspDirective? _manifestSrc;
    private CspDirective? _prefetchSrc;
    private CspDirective? _navigateTo;

    public bool Enabled { get; set; }

    public CspDirective DefaultSrc { get; } = new("default-src", "'self'");

    public CspDirective ScriptSrc => _scriptSrc ??= new CspDirective("script-src");

    public CspDirective StyleSrc => _styleSrc ??= new CspDirective("style-src");

    public CspDirective ImgSrc => _imgSrc ??= new CspDirective("img-src");

    public CspDirective ConnectSrc => _connectSrc ??= new CspDirective("connect-src");

    public CspDirective FontSrc => _fontSrc ??= new CspDirective("font-src");

    public CspDirective ObjectSrc => _objectSrc ??= new CspDirective("object-src");

    public CspDirective MediaSrc => _mediaSrc ??= new CspDirective("media-src");

    public CspDirective FrameSrc => _frameSrc ??= new CspDirective("frame-src");

    public CspDirective Sandbox => _sandbox ??= new CspDirective("sandbox");

    public CspDirective ReportUri => _reportUri ??= new CspDirective("report-uri");

    public CspDirective ChildSrc => _childSrc ??= new CspDirective("child-src");

    public CspDirective FormAction => _formAction ??= new CspDirective("form-action");

    public CspDirective FrameAncestors => _frameAncestors ??= new CspDirective("frame-ancestors");

    public CspDirective PluginTypes => _pluginTypes ??= new CspDirective("plugin-types");

    public CspDirective BaseUri => _baseUri ??= new CspDirective("base-uri");

    public CspDirective ReportTo => _reportTo ??= new CspDirective("report-to");

    public CspDirective WorkerSrc => _workerSrc ??= new CspDirective("worker-src");

    public CspDirective ManifestSrc => _manifestSrc ??= new CspDirective("manifest-src");

    public CspDirective PrefetchSrc => _prefetchSrc ??= new CspDirective("prefetch-src");

    public CspDirective NavigateTo => _navigateTo ??= new CspDirective("navigate-to");

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
        _baseUri?.Write(builder);
        _reportTo?.Write(builder);
        _workerSrc?.Write(builder);
        _manifestSrc?.Write(builder);
        _prefetchSrc?.Write(builder);
        _navigateTo?.Write(builder);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        Write(sb);
        return sb.ToString();
    }
}
