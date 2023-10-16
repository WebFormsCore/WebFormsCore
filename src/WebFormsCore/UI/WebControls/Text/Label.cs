using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebFormsCore.UI.WebControls
{
    public partial class Label : WebControl, ITextControl
    {
        private string? _associatedControlId;

        protected override HtmlTextWriterTag TagKey
            => string.IsNullOrEmpty(AssociatedControlID) ? HtmlTextWriterTag.Span : HtmlTextWriterTag.Label;

        [ViewState] public string Text { get; set; } = string.Empty;

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

        protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
        {
            await base.AddAttributesToRender(writer, token);

            if (AssociatedControlID is not (null or ""))
            {
                var control = FindControl(AssociatedControlID);
                string clientId;

                if (control is null)
                {
                    var logger = Context.RequestServices.GetService<ILogger<Label>>();
                    logger?.LogWarning("Could not find control with ID {ControlID}", AssociatedControlID);
                    clientId = AssociatedControlID;
                }
                else
                {
                    clientId = control.ClientID ?? AssociatedControlID;
                    control.EnsureIdAttribute(); // Ensure the control has a client ID
                }

                writer.AddAttribute(HtmlTextWriterAttribute.For, clientId);
            }
        }

        protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
        {
            return HasControls()
                ? base.RenderContentsAsync(writer, token)
                : writer.WriteAsync(Text);
        }
    }
}
