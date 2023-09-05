using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebFormsCore.UI.WebControls
{
    /// <summary>Serves as the abstract base class for HTML server controls that map to HTML elements that are required to have an opening and a closing tag.</summary>
    public abstract partial class HtmlContainerControl : HtmlControl
    {
        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> class using default values.</summary>
        protected HtmlContainerControl()
            : this("span")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> class using the specified tag name.</summary>
        /// <param name="tag">A string that specifies the tag name of the control. </param>
        public HtmlContainerControl(string tag)
            : base(tag)
        {
        }

        /// <summary>Gets or sets the content found between the opening and closing tags of the specified HTML server control.</summary>
        /// <returns>The HTML content between opening and closing tags of an HTML server control.</returns>
        /// <exception cref="T:System.Web.HttpException">There is more than one HTML server control.- or -The HTML server control is not a <see cref="T:System.Web.UI.LiteralControl" /> or a <see cref="T:System.Web.UI.DataBoundLiteralControl" />. </exception>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual string InnerHtml
        {
            get
            {
                if (Controls.Count == 1 && Controls[0] is LiteralControl textControl)
                {
                    return textControl.Text;
                }

                if (Controls.Count == 0)
                {
                    return string.Empty;
                }

                throw new InvalidOperationException("InnerHtml can only be get when there is a single LiteralControl child.");
            }
            set
            {
                if (Controls.Count == 1 && Controls[0] is LiteralControl textControl)
                {
                    textControl.Text = value;
                }
                else if (Controls.Count > 0)
                {
                    throw new InvalidOperationException("InnerHtml can only be set when there is a single LiteralControl child.");
                }
                else
                {
                    Controls.AddWithoutPageEvents(WebActivator.CreateLiteral(value));
                }
            }
        }

        /// <summary>Gets or sets the text between the opening and closing tags of the specified HTML server control.</summary>
        /// <returns>The text between the opening and closing tags of an HTML server control.</returns>
        /// <exception cref="T:System.Web.HttpException">There is more than one HTML server control.- or - The HTML server control is not a <see cref="T:System.Web.UI.LiteralControl" /> or a <see cref="T:System.Web.UI.DataBoundLiteralControl" />. </exception>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual string InnerText
        {
            get => HttpUtility.HtmlDecode(InnerHtml);
            set => InnerHtml = HttpUtility.HtmlEncode(value);
        }

        /// <summary>Creates a new <see cref="T:WebFormsCore.UI.ControlCollection" /> object to hold the child controls (both literal and server) of the server control.</summary>
        /// <returns>A <see cref="T:WebFormsCore.UI.ControlCollection" /> that contains the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> child server controls.</returns>
        protected override ControlCollection CreateControlCollection() => new(this);

        /// <summary>Renders the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> control to the specified <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object.</summary>
        /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that receives the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> content.</param>
        /// <param name="token"></param>
        public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            var isVoidTag = TagName is "area" or "base" or "br" or "col" or "command" or "embed"
                or "hr" or "img" or "input" or "keygen" or "link" or "meta"
                or "param" or "source" or "track" or "wbr";

            if (isVoidTag)
            {
                await writer.WriteBeginTagAsync(TagName);
                await RenderAttributesAsync(writer);
                await writer.WriteAsync(" />");
            }
            else
            {
                await RenderBeginTagAsync(writer, token);
                await RenderChildrenAsync(writer, token);
                await RenderEndTagAsync(writer, token);
            }
        }

        /// <summary>Renders the closing tag for the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> control to the specified <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object.</summary>
        /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that receives the rendered content.</param>
        protected virtual void RenderEndTag(HtmlTextWriter writer) => writer.WriteEndTag(TagName);

        /// <summary>Renders the closing tag for the <see cref="T:WebFormsCore.UI.WebControls.HtmlContainerControl" /> control to the specified <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object.</summary>
        /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that receives the rendered content.</param>
        /// <param name="cancellationToken"></param>
        protected virtual ValueTask RenderEndTagAsync(HtmlTextWriter writer, CancellationToken cancellationToken) => writer.WriteEndTagAsync(TagName);
    }
}
