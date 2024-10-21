using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using WebFormsCore.Security;
using ScriptList = System.Collections.Generic.List<WebFormsCore.UI.RegisteredScript>;

namespace WebFormsCore.UI;

internal class RegisteredScript : IResettable
{
    public (ScriptType ScriptType, Type Type, string Key) Key { get; set; }
    public string Script { get; set; } = string.Empty;
    public string? Nonce { get; set; }
    public RegisterType Type { get; set; }
    public IAttributeRenderer? Attributes { get; set; }

    public void Deconstruct(out string script, out string? nonce, out RegisterType type, out IAttributeRenderer? attributes)
    {
        script = Script;
        nonce = Nonce;
        type = Type;
        attributes = Attributes;
    }

    public bool TryReset()
    {
        Key = default;
        Script = string.Empty;
        Nonce = null;
        Type = RegisterType.Raw;
        Attributes = null;
        return true;
    }
}

internal enum RegisterType
{
    Raw,
    InlineScript,
    InlineStyle,
    ExternalScript,
    ExternalStyle
}

public enum ScriptType
{
    Script,
    Style
}

public enum ScriptPosition
{
    /// <summary>
    /// The script is registered right after the opening &lt;head&gt; tag.
    /// </summary>
    HeadStart,

    /// <summary>
    /// The script is registered right before the closing &lt;/head&gt; tag.
    /// </summary>
    HeadEnd,

    /// <summary>
    /// The script is registered right after the opening &lt;body&gt; tag.
    /// </summary>
    BodyStart,

    /// <summary>
    /// The script is registered right before the closing &lt;/body&gt; tag.
    /// </summary>
    BodyEnd
}

public sealed class ClientScriptManager(Page page, IOptions<WebFormsCoreOptions>? options = null)
{
    private static readonly ObjectPool<RegisteredScript> RegisteredScriptPool = new DefaultObjectPool<RegisteredScript>(new DefaultPooledObjectPolicy<RegisteredScript>());

    private const string ScriptStart = "<script";
    private const string ScriptEnd = "</script>\n";
    private const string StyleStart = "<style";
    private const string StyleEnd = "</style>\n";
    private const string LinkStart = "<link rel=\"stylesheet\"";
    private const string LinkEnd = " />\n";

    private readonly Dictionary<(ScriptType, Type, string), (ScriptList list, RegisteredScript script)> _registeredScripts = new();
    private ScriptList? _headStart;
    private ScriptList? _headEnd;
    private ScriptList? _bodyEnd;
    private ScriptList? _bodyStart;
    private readonly ScriptPosition _defaultScriptPosition = options?.Value.DefaultScriptPosition ?? ScriptPosition.BodyEnd;
    private readonly ScriptPosition _defaultStylePosition = options?.Value.DefaultStylePosition ?? ScriptPosition.HeadEnd;

    private ref ScriptList? GetList(ScriptPosition position)
    {
        switch (position)
        {
            case ScriptPosition.HeadStart:
                return ref _headStart;
            case ScriptPosition.HeadEnd:
                return ref _headEnd;
            case ScriptPosition.BodyStart:
                return ref _bodyStart;
            case ScriptPosition.BodyEnd:
                return ref _bodyEnd;
            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }
    }

    public bool IsStartupScriptRegistered(string key)
    {
        return IsStartupScriptRegistered(typeof(Page), key);
    }

    private bool IsStartupScriptRegistered(Type type, string key)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return _registeredScripts.ContainsKey((ScriptType.Script, type, key));
    }

    public void RegisterStartupScript(Type type, string key, [LanguageInjection(InjectedLanguage.JAVASCRIPT)] string script, bool addScriptTags = true, IAttributeRenderer? attributes = null, ScriptPosition? position = null)
    {
        RegisterBlock(ScriptType.Script, type, key, script, ref GetList(position ?? _defaultScriptPosition), addScriptTags ? RegisterType.InlineScript : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStaticScript(Type type, PathString fileName, [LanguageInjection(InjectedLanguage.JAVASCRIPT)] string script, bool addScriptTags = true, IAttributeRenderer? linkAttributes = null, ScriptPosition? position = null)
    {
        if (StaticFiles.Files.TryGetValue(fileName, out var existingScript))
        {
            if (!string.Equals(existingScript, script, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The file {fileName} is already registered.");
            }
        }
        else
        {
            StaticFiles.Files[fileName] = script;
        }

        if (page.Context.Items.ContainsKey("RegisteredWebFormsCore"))
        {
            RegisterStartupScriptLink(type, fileName, fileName, addScriptTags, linkAttributes, position);
        }
        else
        {
            RegisterStartupScript(type, fileName, script, addScriptTags, position: position);
        }
    }

    public void RegisterStartupScriptLink(Type type, string key, string url, bool addScriptTags = true, IAttributeRenderer? attributes = null, ScriptPosition? position = null)
    {
        RegisterBlock(ScriptType.Script, type, key, url, ref GetList(position ?? _defaultScriptPosition), addScriptTags ? RegisterType.ExternalScript : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStyle(Type type, string key, string content, bool addStyleTags, IAttributeRenderer? attributes = null, ScriptPosition? position = null)
    {
        RegisterBlock(ScriptType.Style, type, key, content, ref GetList(position ?? _defaultStylePosition), addStyleTags ? RegisterType.InlineStyle : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStyleLink(Type type, string key, string url, bool addStyleTags = true, IAttributeRenderer? attributes = null, ScriptPosition? position = null)
    {
        RegisterBlock(ScriptType.Style, type, key, url, ref GetList(position ?? _defaultStylePosition), addStyleTags ? RegisterType.ExternalStyle : RegisterType.Raw, attribute: attributes);
    }

    private void RegisterBlock(ScriptType scriptType, Type type, string key, string content, ref ScriptList? list, RegisterType registerType, IAttributeRenderer? attribute)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var dictionaryKey = (scriptType, type, key);

        if (list != null && _registeredScripts.ContainsKey(dictionaryKey))
        {
            return;
        }

        var registration = RegisteredScriptPool.Get();
        registration.Key = dictionaryKey;
        registration.Script = content;
        registration.Type = registerType;
        registration.Attributes = attribute;

        list ??= new ScriptList();
        list.Add(registration);

        _registeredScripts[dictionaryKey] = (list, registration);
    }

    internal void OnPreRender()
    {
        if (!page.Csp.Enabled)
        {
            return;
        }

        foreach (var kv in _registeredScripts)
        {
            var (content, cspNonce, type, _) = kv.Value.script;

            if (cspNonce is not null || type is RegisterType.Raw)
            {
                continue;
            }

            var cspTarget = type is RegisterType.ExternalScript or RegisterType.InlineScript
                ? page.Csp.ScriptSrc
                : page.Csp.StyleSrc;

            if (type is RegisterType.ExternalScript or RegisterType.ExternalStyle)
            {
                if (cspTarget.Mode.HasFlag(CspMode.Uri) && Uri.TryCreate(content, UriKind.RelativeOrAbsolute, out var href))
                {
                    cspTarget.Add(href.IsAbsoluteUri ? $"{href.Scheme}://{href.Host}" : "'self'");
                }
                else if (cspTarget.Mode.HasFlag(CspMode.Nonce))
                {
                    kv.Value.script.Nonce = cspTarget.GenerateNonce();
                }
                else
                {
                    throw new InvalidOperationException("Cannot register CSP with the current configuration.");
                }
            }
            else if (cspTarget.Mode.HasFlag(CspMode.Sha256))
            {
                if (content.IndexOf('\r') != -1)
                {
                    // TODO: Reduce allocations
                    content = content.ReplaceLineEndings("\n");
                }

                page.Csp.ScriptSrc.AddInlineHash(content);
            }
            else if (cspTarget.Mode.HasFlag(CspMode.Nonce))
            {
                kv.Value.script.Nonce = cspTarget.GenerateNonce();
            }
            else
            {
                throw new InvalidOperationException("Cannot register CSP with the current configuration.");
            }
        }
    }

    private async ValueTask Render(HtmlTextWriter writer, ScriptList scripts, ScriptType scriptType)
    {
        await writer.WriteLineAsync();
        var current = RegisterType.Raw;

        for (var index = 0; index < scripts.Count; index++)
        {
            var kv = scripts[index];
            var (script, cspNonce, type, attributes) = kv;

            if (kv.Key.ScriptType != scriptType)
            {
                continue;
            }

            var attr = type switch
            {
                RegisterType.ExternalScript => "src",
                RegisterType.ExternalStyle => "href",
                _ => null
            };

            if (current != type)
            {
                await WriteEndTag(writer, current);
                await WriteBeginTag(writer, type);

                if (cspNonce != null)
                {
                    await writer.WriteAsync(" nonce=\"");
                    await writer.WriteAsync(cspNonce);
                    await writer.WriteAsync('"');
                }

                if (attributes != null)
                {
                    await attributes.RenderAsync(writer);
                }

                if (attr != null)
                {
                    await writer.WriteAsync(' ');
                    await writer.WriteAsync(attr);
                    await writer.WriteAsync("=\"");
                    await writer.WriteAsync(script);
                    await writer.WriteAsync('"');
                }

                if (type is not RegisterType.ExternalStyle)
                {
                    await writer.WriteAsync('>');
                }

                current = type;
            }

            if (attr == null)
            {
                await writer.WriteAsync(script);
            }

            if (attr != null || page.Csp.Enabled)
            {
                await WriteEndTag(writer, current);
                current = RegisterType.Raw;
            }

            _registeredScripts.Remove(kv.Key);
            scripts.RemoveAt(index--);
            RegisteredScriptPool.Return(kv);
        }

        await WriteEndTag(writer, current);
    }

    private static ValueTask WriteEndTag(HtmlTextWriter writer, RegisterType current)
    {
        return current switch
        {
            RegisterType.InlineScript => writer.WriteAsync(ScriptEnd),
            RegisterType.ExternalScript => writer.WriteAsync(ScriptEnd),
            RegisterType.InlineStyle => writer.WriteAsync(StyleEnd),
            RegisterType.ExternalStyle => writer.WriteAsync(LinkEnd),
            RegisterType.Raw => default,
            _ => throw new ArgumentOutOfRangeException(nameof(current), current, null)
        };
    }

    private static ValueTask WriteBeginTag(HtmlTextWriter writer, RegisterType current)
    {
        return current switch
        {
            RegisterType.InlineScript => writer.WriteAsync(ScriptStart),
            RegisterType.ExternalScript => writer.WriteAsync(ScriptStart),
            RegisterType.InlineStyle => writer.WriteAsync(StyleStart),
            RegisterType.ExternalStyle => writer.WriteAsync(LinkStart),
            RegisterType.Raw => default,
            _ => throw new ArgumentOutOfRangeException(nameof(current), current, null)
        };
    }

    public ValueTask RenderHeadStart(HtmlTextWriter writer, ScriptType scriptType)
    {
        return _headStart is null ? default : Render(writer, _headStart, scriptType);
    }

    public ValueTask RenderHeadEnd(HtmlTextWriter writer, ScriptType scriptType)
    {
        return _headEnd is null ? default : Render(writer, _headEnd, scriptType);
    }

    public ValueTask RenderBodyStart(HtmlTextWriter writer, ScriptType scriptType)
    {
        return _bodyStart is null ? default : Render(writer, _bodyStart, scriptType);
    }

    public ValueTask RenderBodyEnd(HtmlTextWriter writer, ScriptType scriptType)
    {
        return _bodyEnd is null ? default : Render(writer, _bodyEnd, scriptType);
    }
}