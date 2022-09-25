using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI.WebControls
{
    public class HtmlForm : HtmlContainerControl, INamingContainer
    {
        public HtmlForm()
            : base("form")
        {
        }
        
        public bool Global { get; set; }

        protected override async Task OnInitAsync(CancellationToken token)
        {
            await base.OnInitAsync(token);

            Page.Forms.Add(this);
        }

        internal virtual Control ViewStateOwner => Global ? Page : this;

        protected override async Task RenderAttributesAsync(HtmlTextWriter writer)
        {
            await base.RenderAttributesAsync(writer);
            await writer.WriteAttributeAsync("data-wfc-form", Global ? "global" : "scope");
        }

        protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
        {
            var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

            await base.RenderChildrenAsync(writer, token);

            await writer.WriteAsync(@"<input type=""hidden"" name=""__FORM"" value=""");
            await writer.WriteAsync(UniqueID);
            await writer.WriteAsync(@"""/>");


            using var viewState = viewStateManager.Write(this, out var length);

            await writer.WriteAsync(@"<input type=""hidden"" name=""__VIEWSTATE"" value=""");
            await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
            await writer.WriteAsync(@"""/>");
        }
    }

    public class PlaceHolder : Control
    {
    }
}
