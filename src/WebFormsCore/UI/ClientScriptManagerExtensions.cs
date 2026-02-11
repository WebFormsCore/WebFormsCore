using System;
using System.Threading.Tasks;

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
