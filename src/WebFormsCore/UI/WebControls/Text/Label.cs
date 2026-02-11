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
        private string _text = string.Empty;
        private bool _textSetByAddParsedSubObject;

        protected override HtmlTextWriterTag TagKey
            => string.IsNullOrEmpty(AssociatedControlID) ? HtmlTextWriterTag.Span : HtmlTextWriterTag.Label;

        /// <summary>
        /// Gets or sets the text content of the Label.
        /// Setting this property clears any child controls so the new text
        /// always takes effect, matching classic ASP.NET behavior.
        /// </summary>
        [ViewState]
        public string Text
        {
            get => _text;
            set
            {
                if (HasControls())
                {
                    Controls.Clear();
                }

                _text = value ?? string.Empty;
            }
        }

        [DefaultValue("")]
        [IDReferenceProperty]
        [ViewState]
        public virtual string? AssociatedControlID
        {
            get => _associatedControlId;
            set => _associatedControlId = value;
        }

        /// <summary>
        /// Intercepts parsed sub-objects so that literal children
        /// (e.g. <c>&lt;wfc:Label&gt;Hello&lt;/wfc:Label&gt;</c>) set
        /// <see cref="Text"/> instead of being added as child controls.
        /// When non-literal children are present, falls back to the default
        /// behavior and moves any accumulated text into a <see cref="LiteralControl"/>.
        /// This matches classic ASP.NET Label behavior.
        /// </summary>
        public override void AddParsedSubObject(Control control)
        {
            if (HasControls())
            {
                base.AddParsedSubObject(control);
            }
            else if (control is LiteralControl literal)
            {
                if (_textSetByAddParsedSubObject)
                {
                    _text += literal.Text;
                }
                else
                {
                    _text = literal.Text;
                }

                _textSetByAddParsedSubObject = true;
            }
            else
            {
                // Non-literal child: move any existing text into a LiteralControl
                // and switch to child-control mode.
                var currentText = Text;

                if (currentText.Length != 0)
                {
                    _text = string.Empty;
                    base.AddParsedSubObject(new LiteralControl { Text = currentText });
                }

                base.AddParsedSubObject(control);
            }
        }

        public override void ClearControl()
        {
            base.ClearControl();

            _text = string.Empty;
            _associatedControlId = null;
            _textSetByAddParsedSubObject = false;
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
