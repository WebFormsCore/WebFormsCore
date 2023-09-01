using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls
{
    /// <summary>Defines the methods, properties, and events common to all HTML server controls in the ASP.NET page framework.</summary>
    [Designer("System.Web.UI.Design.HtmlIntrinsicControlDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ToolboxItem(false)]
    public abstract partial class HtmlControl : Control, IAttributeAccessor
    {
        internal string _tagName;
        private string _initialTagName;
        [ViewState] private AttributeCollection _attributes = new();

        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> class using default values.</summary>
        protected HtmlControl()
            : this("span")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> class using the specified tag.</summary>
        /// <param name="tag">A string that specifies the tag name of the control. </param>
        protected HtmlControl(string tag)
        {
            _tagName = tag;
            _initialTagName = tag;
        }

        /// <summary>Gets a collection of all attribute name and value pairs expressed on a server control tag within the ASP.NET page.</summary>
        /// <returns>A <see cref="T:WebFormsCore.UI.AttributeCollection" /> object that contains all attribute name and value pairs expressed on a server control tag within the Web page.</returns>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AttributeCollection Attributes => _attributes;

        /// <summary>Gets a collection of all cascading style sheet (CSS) properties applied to a specified HTML server control in the ASP.NET file.</summary>
        /// <returns>A <see cref="T:WebFormsCore.UI.CssStyleCollection" /> object that contains the style properties for the HTML server control.</returns>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CssStyleCollection Style => Attributes.CssStyle;

        /// <summary>Gets the element name of a tag that contains a <see langword="runat=server" /> attribute and value pair.</summary>
        /// <returns>The element name of the specified tag.</returns>
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual string TagName => _tagName;

        /// <summary>Gets or sets a value indicating whether the HTML server control is disabled.</summary>
        /// <returns>
        /// <see langword="true" /> if the control is disabled; otherwise, <see langword="false" />. The default value is <see langword="false" />.</returns>
        [DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [TypeConverter(typeof(MinimizableAttributeTypeConverter))]
        public bool Disabled
        {
            get => Attributes["disabled"] is "disabled";
            set => Attributes["disabled"] = value ? "disabled" : null;
        }

        /// <summary>Gets a value that indicates whether the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> view state is case-sensitive.</summary>
        /// <returns>
        /// <see langword="true" /> in all cases.</returns>
        protected override bool ViewStateIgnoresCase => true;

        /// <summary>Creates a new <see cref="T:WebFormsCore.UI.ControlCollection" /> object to hold the child controls (both literal and server) of the server control.</summary>
        /// <returns>A <see cref="T:WebFormsCore.UI.ControlCollection" /> object to contain the current server control's child server controls.</returns>
        protected override ControlCollection CreateControlCollection() => new EmptyControlCollection(this);

        public override Task RenderAsync(HtmlTextWriter writer, CancellationToken token) => RenderBeginTagAsync(writer, token);

        /// <summary>Renders the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> control's attributes into the specified <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object.</summary>
        /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that receives the rendered content.</param>
        protected virtual async Task RenderAttributesAsync(HtmlTextWriter writer)
        {
            if (ClientID != null)
            {
                await writer.WriteAttributeAsync("id", ClientID);
            }

            await Attributes.RenderAsync(writer);
        }

        /// <summary>Renders the opening HTML tag of the control into the specified <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> object.</summary>
        /// <param name="writer">The <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that receives the rendered content.</param>
        protected virtual async Task RenderBeginTagAsync(HtmlTextWriter writer, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            await writer.WriteBeginTagAsync(TagName);
            await RenderAttributesAsync(writer);
            await writer.WriteAsync('>');
        }

        /// <summary>For a description of this member, see <see cref="M:WebFormsCore.UI.IAttributeAccessor.GetAttribute(System.String)" />. </summary>
        /// <param name="name">The attribute name.</param>
        /// <returns>The value of this attribute on the element, as a <see cref="T:System.String" /> value. If the specified attribute does not exist on this element, returns an empty string ("").</returns>
        string? IAttributeAccessor.GetAttribute(string name) => GetAttribute(name);

        /// <summary>Gets the value of the named attribute on the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> control.</summary>
        /// <param name="name">The name of the attribute. This argument is case-insensitive.</param>
        /// <returns>The value of this attribute on the element, as a <see cref="T:System.String" /> value. If the specified attribute does not exist on this element, returns an empty string ("").</returns>
        protected virtual string? GetAttribute(string name) => Attributes[name];

        /// <summary>For a description of this member, see <see cref="M:WebFormsCore.UI.IAttributeAccessor.SetAttribute(System.String,System.String)" />. </summary>
        /// <param name="name">The name of the attribute to set.</param>
        /// <param name="value">The value to set the attribute to.</param>
        void IAttributeAccessor.SetAttribute(string name, string value) => SetAttribute(name, value);

        /// <summary>Sets the value of the named attribute on the <see cref="T:WebFormsCore.UI.WebControls.HtmlControl" /> control.</summary>
        /// <param name="name">The name of the attribute to set.</param>
        /// <param name="value">The value to set the attribute to.</param>
        protected virtual void SetAttribute(string name, string value) => Attributes[name] = value;

        public override void ClearControl()
        {
            base.ClearControl();

            _tagName = _initialTagName;
            _attributes?.Clear();
            _attributes?.CssStyle.Clear();
        }
    }
}
