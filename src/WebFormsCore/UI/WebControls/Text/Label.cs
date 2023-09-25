using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;

namespace WebFormsCore.UI.WebControls
{
    public partial class Label : WebControl, ITextControl
    {
        private string? _associatedControlId;

        [ViewState] public string Text { get; set; } = string.Empty;

        /// <devdoc>
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        [DefaultValue("")]
        [IDReferenceProperty]
        [ViewState]
        public virtual string? AssociatedControlID
        {
            get => _associatedControlId;
            set => _associatedControlId = value;
        }

        public override void ClearControl()
        {
            base.ClearControl();

            Text = string.Empty;
            _associatedControlId = null;
        }

        protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
        {
            return HasControls()
                ? base.RenderContentsAsync(writer, token)
                : writer.WriteAsync(Text);
        }
    }
}
