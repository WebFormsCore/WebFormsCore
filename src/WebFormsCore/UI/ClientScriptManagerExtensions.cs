using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore.UI;

public static class ClientScriptManagerExtensions
{
    public static void RegisterStartupDeferScriptLink(this ClientScriptManager manager, Type type, string key, string url, bool addScriptTags = true, ScriptPosition? position = null)
    {
        manager.RegisterStartupScriptLink(type, key, url, addScriptTags, SingleAttributeRenderer.Defer, position);
    }

    public static void RegisterStartupAsyncScriptLink(this ClientScriptManager manager, Type type, string key, string url, bool addScriptTags = true, ScriptPosition? position = null)
    {
        manager.RegisterStartupScriptLink(type, key, url, addScriptTags, SingleAttributeRenderer.Async, position);
    }

    public static void RegisterStartupDeferStaticScript(this ClientScriptManager manager, Type type, PathString fileName, string url, bool addScriptTags = true, ScriptPosition? position = null)
    {
        manager.RegisterStartupStaticScript(type, fileName, url, addScriptTags, SingleAttributeRenderer.Defer, position);
    }

    private sealed class SingleAttributeRenderer(string attributeName) : IAttributeRenderer
    {
        public static readonly SingleAttributeRenderer Defer = new("defer");
        public static readonly SingleAttributeRenderer Async = new("async");

        public async ValueTask RenderAsync(HtmlTextWriter writer)
        {
            await writer.WriteAttributeAsync(attributeName, null);
        }

        public void AddAttributes(HtmlTextWriter writer)
        {
            writer.AddAttribute(attributeName, null);
        }
    }
}
