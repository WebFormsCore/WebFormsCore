using System;
using System.Threading.Tasks;
using HttpStack;

namespace WebFormsCore.UI;

public static class ClientScriptManagerExtensions
{
    public static void RegisterStartupDeferScriptLink(this ClientScriptManager manager, Type type, string key, string url, bool addScriptTags = true)
    {
        manager.RegisterStartupScriptLink(type, key, url, addScriptTags, SingleAttributeRenderer.Defer);
    }

    public static void RegisterStartupAsyncScriptLink(this ClientScriptManager manager, Type type, string key, string url, bool addScriptTags = true)
    {
        manager.RegisterStartupScriptLink(type, key, url, addScriptTags, SingleAttributeRenderer.Async);
    }

    public static void RegisterStartupDeferStaticScript(this ClientScriptManager manager, Type type, PathString fileName, string url, bool addScriptTags = true)
    {
        manager.RegisterStartupStaticScript(type, fileName, url, addScriptTags, SingleAttributeRenderer.Defer);
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
