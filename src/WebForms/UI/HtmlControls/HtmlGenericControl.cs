using System.ComponentModel;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls
{
    /// <summary>Defines the methods, properties, and events for all HTML server control elements not represented by a specific .NET Framework class.</summary>
    public class HtmlGenericControl : HtmlContainerControl
    {
        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> class with default values.</summary>
        public HtmlGenericControl()
            : this("span")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> class with the specified tag.</summary>
        /// <param name="tag">The name of the element for which this instance of the class is created. </param>
        public HtmlGenericControl(string tag)
            : base(tag)
        {
        }

        /// <summary>Gets or sets the name of the HTML element represented by the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> control.</summary>
        /// <returns>The tag name of an element.</returns>
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string TagName
        {
            get => _tagName;
            set => _tagName = value;
        }
    }
}
