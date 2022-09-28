using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using WebFormsCore.Security;
using ScriptDictionary = System.Collections.Generic.Dictionary<(System.Type, string), WebFormsCore.UI.RegisteredScript>;

namespace WebFormsCore.UI;

internal record struct RegisteredScript(string Script, bool NeedsScriptTags, string? Nonce);

public sealed class ClientScriptManager
{
    private const string ScriptStart = "\n<script";
    private const string ScriptEnd = "</script>\n";

    private readonly Page _page;
    private ScriptDictionary? _registeredClientStartupScripts;

    public ClientScriptManager(Page page)
    {
        _page = page;
    }

    public bool IsStartupScriptRegistered(string key)
    {
        return IsStartupScriptRegistered(typeof(Page), key);
    }

    public bool IsStartupScriptRegistered(Type type, string key)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return _registeredClientStartupScripts != null &&
               _registeredClientStartupScripts.ContainsKey((type, key));
    }

    public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        RegisterScriptBlock(type, key, script, ref _registeredClientStartupScripts, addScriptTags);
    }

    private void RegisterScriptBlock(Type type, string key, string script, ref ScriptDictionary? dictionary, bool addScriptTags)
    {
        var cspMode = _page.Csp.ScriptSrc.Mode;
        var nonce = cspMode is CspMode.Nonce ? _page.Csp.ScriptSrc.GenerateNonce() : null;

        if (addScriptTags && cspMode is CspMode.Sha256)
        {
            _page.Csp.ScriptSrc.AddInlineHash(script);
        }

        dictionary ??= new ScriptDictionary();
        dictionary[(type, key)] = new RegisteredScript(script, addScriptTags, nonce);
    }

    private async ValueTask RenderRegisteredScripts(HtmlTextWriter writer, ScriptDictionary? scripts)
    {
        if (scripts == null)
        {
            return;
        }

        await writer.WriteLineAsync();
        bool inScriptBlock = false;
        // Write out each registered script block
        foreach (var kv in scripts)
        {
            var (script, needsScriptTags, cspNonce) = kv.Value;

            if (needsScriptTags)
            {
                if (!inScriptBlock)
                {
                    await writer.WriteAsync(ScriptStart);

                    if (cspNonce != null)
                    {
                        await writer.WriteAsync(" nonce=\"");
                        await writer.WriteAsync(cspNonce);
                        await writer.WriteAsync('"');
                    }

                    await writer.WriteAsync('>');

                    inScriptBlock = true;
                }
            }
            else
            {
                await writer.WriteAsync(ScriptEnd);
                inScriptBlock = false;
            }

            await writer.WriteAsync(script);

            if (_page.Csp.Enabled)
            {
                await writer.WriteAsync(ScriptEnd);
                inScriptBlock = false;
            }
        }

        if (inScriptBlock)
        {
            await writer.WriteAsync(ScriptEnd);
        }
    }

    internal ValueTask RenderStartupScripts(HtmlTextWriter writer)
    {
        return RenderRegisteredScripts(writer, _registeredClientStartupScripts);
    }
}
