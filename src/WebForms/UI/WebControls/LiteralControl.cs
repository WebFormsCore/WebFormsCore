// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace System.Web.UI
{
    /// <summary>Represents HTML elements, text, and any other strings in an ASP.NET page that do not require processing on the server.</summary>
    [ToolboxItem(false)]
    public sealed class LiteralControl : Control, ITextControl
    {
        private string _text;

        public LiteralControl()
        {
            _text = string.Empty;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.LiteralControl" /> class with the specified text.</summary>
        /// <param name="text">The text to be rendered on the requested Web page. </param>
        public LiteralControl(string? text)
        {
            _text = text ?? string.Empty;
        }

        /// <summary>Gets or sets the text content of the <see cref="T:System.Web.UI.LiteralControl" /> object.</summary>
        /// <returns>A <see cref="T:System.String" /> that represents the text content of the literal control. The default is <see cref="F:System.String.Empty" />.</returns>
        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        /// <summary>Creates an <see cref="T:System.Web.UI.EmptyControlCollection" /> object for the current instance of the <see cref="T:System.Web.UI.LiteralControl" /> class.</summary>
        /// <returns>The <see cref="T:System.Web.UI.EmptyControlCollection" /> for the current control.</returns>
        protected override ControlCollection CreateControlCollection() => new EmptyControlCollection(this);

        /// <summary>Writes the content of the <see cref="T:System.Web.UI.LiteralControl" /> object to the ASP.NET page.</summary>
        /// <param name="output">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that renders the content of the <see cref="T:System.Web.UI.LiteralControl" /> to the requesting client. </param>
        public override ValueTask RenderAsync(HtmlTextWriter output, CancellationToken token) => new(output.WriteAsync(_text));

        public override void ClearControl()
        {
            base.ClearControl();
            _text = string.Empty;
        }

        protected override void OnWriteViewState(ref ViewStateWriter writer)
        {
        }

        protected override void OnReadViewState(ref ViewStateReader reader)
        {
        }
    }
}
