using System;
using System.Threading.Tasks;
using HttpStack;
using JetBrains.Annotations;
using WebFormsCore.Security;
using ScriptDictionary = System.Collections.Generic.Dictionary<(System.Type, string), WebFormsCore.UI.RegisteredScript>;

namespace WebFormsCore.UI;

internal record struct RegisteredScript(string Script, string? Nonce, RegisterType Type, IAttributeRenderer? Attributes);

internal enum RegisterType
{
    Raw,
    InlineScript,
    InlineStyle,
    ExternalScript,
    ExternalStyle
}

public sealed class ClientScriptManager
{
    private const string ScriptStart = "<script";
    private const string ScriptEnd = "</script>\n";
    private const string StyleStart = "<style";
    private const string StyleEnd = "</style>\n";
    private const string LinkStart = "<link rel=\"stylesheet\"";
    private const string LinkEnd = " />\n";

    private readonly Page _page;
    private ScriptDictionary? _startupBody;
    private ScriptDictionary? _startupHead;

    public ClientScriptManager(Page page)
    {
        _page = page;
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

        return _startupBody != null &&
               _startupBody.ContainsKey((type, key));
    }

    public void RegisterHeadScript(Type type, string key, [LanguageInjection(InjectedLanguage.JAVASCRIPT)] string script, bool addScriptTags = true, IAttributeRenderer? attributes = null)
    {
        RegisterBlock(type, key, script, ref _startupHead, addScriptTags ? RegisterType.InlineScript : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupScript(Type type, string key, [LanguageInjection(InjectedLanguage.JAVASCRIPT)] string script, bool addScriptTags = true, IAttributeRenderer? attributes = null)
    {
        RegisterBlock(type, key, script, ref _startupBody, addScriptTags ? RegisterType.InlineScript : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStaticScript(Type type, PathString fileName, [LanguageInjection(InjectedLanguage.JAVASCRIPT)] string script, bool addScriptTags = true, IAttributeRenderer? linkAttributes = null)
    {
        if (StaticFiles.Files.TryGetValue(fileName, out var existingScript))
        {
            if (existingScript.GetHashCode() != script.GetHashCode() && existingScript != script)
            {
                throw new InvalidOperationException($"The file {fileName} is already registered.");
            }
        }
        else
        {
            StaticFiles.Files[fileName] = script;
        }

        if (_page.Context.Items.ContainsKey("RegisteredWebFormsCore"))
        {
            RegisterStartupScriptLink(type, fileName, fileName, addScriptTags, linkAttributes);
        }
        else
        {
            RegisterStartupScript(type, fileName, script, addScriptTags);
        }
    }

    public void RegisterStartupScriptLink(Type type, string key, string url, bool addScriptTags = true, IAttributeRenderer? attributes = null)
    {
        RegisterBlock(type, key, url, ref _startupBody, addScriptTags ? RegisterType.ExternalScript : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStyle(Type type, string key, string content, bool addStyleTags, IAttributeRenderer? attributes = null)
    {
        RegisterBlock(type, key, content, ref _startupHead, addStyleTags ? RegisterType.InlineStyle : RegisterType.Raw, attribute: attributes);
    }

    public void RegisterStartupStyleLink(Type type, string key, string url, bool addStyleTags = true, IAttributeRenderer? attributes = null)
    {
        RegisterBlock(type, key, url, ref _startupHead, addStyleTags ? RegisterType.ExternalStyle : RegisterType.Raw, attribute: attributes);
    }

    private void RegisterBlock(Type type, string key, string content, ref ScriptDictionary? dictionary, RegisterType registerType, IAttributeRenderer? attribute)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var dictionaryKey = (type, key);

        if (dictionary != null && dictionary.ContainsKey(dictionaryKey))
        {
            return;
        }

        string? nonce = null;

        if (_page.Csp.Enabled && registerType is not RegisterType.Raw)
        {
            var cspTarget = registerType is RegisterType.ExternalScript or RegisterType.InlineScript
                ? _page.Csp.ScriptSrc
                : _page.Csp.StyleSrc;

            if (registerType is RegisterType.ExternalScript or RegisterType.ExternalStyle)
            {
                if (Uri.TryCreate(content, UriKind.Absolute, out var href))
                {
                    cspTarget.Add($"{href.Scheme}://{href.Host}");
                }
            }
            else if (cspTarget.Mode is CspMode.Nonce)
            {
                nonce = cspTarget.GenerateNonce();
            }
            else if (cspTarget.Mode is CspMode.Sha256)
            {
                if (content.IndexOf('\r') != -1)
                {
                    content = content.ReplaceLineEndings("\n");
                }

                _page.Csp.ScriptSrc.AddInlineHash(content);
            }
        }

        dictionary ??= new ScriptDictionary();
        dictionary[dictionaryKey] = new RegisteredScript(content, nonce, registerType, attribute);
    }

    private async ValueTask Render(HtmlTextWriter writer, ScriptDictionary? scripts)
    {
        if (scripts == null)
        {
            return;
        }

        await writer.WriteLineAsync();
        var current = RegisterType.Raw;

        foreach (var kv in scripts)
        {
            var (script, cspNonce, type, attributes) = kv.Value;
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

            if (attr != null || _page.Csp.Enabled)
            {
                await WriteEndTag(writer, current);
                current = RegisterType.Raw;
            }
        }

        await WriteEndTag(writer, current);
    }

    private static ValueTask WriteEndTag(HtmlTextWriter writer, RegisterType current)
    {
        switch (current)
        {
            case RegisterType.InlineScript:
            case RegisterType.ExternalScript:
                return writer.WriteAsync(ScriptEnd);
            case RegisterType.InlineStyle:
                return writer.WriteAsync(StyleEnd);
            case RegisterType.ExternalStyle:
                return writer.WriteAsync(LinkEnd);
            default:
                return default;
        }
    }

    private static ValueTask WriteBeginTag(HtmlTextWriter writer, RegisterType current)
    {
        switch (current)
        {
            case RegisterType.InlineScript:
            case RegisterType.ExternalScript:
                return writer.WriteAsync(ScriptStart);
            case RegisterType.InlineStyle:
                return writer.WriteAsync(StyleStart);
            case RegisterType.ExternalStyle:
                return writer.WriteAsync(LinkStart);
            default:
                return default;
        }
    }

    internal ValueTask RenderStartupHead(HtmlTextWriter writer)
    {
        return Render(writer, _startupHead);
    }

    internal ValueTask RenderStartupBody(HtmlTextWriter writer)
    {
        return Render(writer, _startupBody);
    }
}
