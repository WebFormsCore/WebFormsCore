using System.ComponentModel;

namespace System.Web;

[AttributeUsage(AttributeTargets.All)]
internal class WebSysDescriptionAttribute : DescriptionAttribute
{
    private bool _replaced;

    internal WebSysDescriptionAttribute(string description)
        : base(description)
    {
    }

    public override string Description
    {
        get
        {
            if (!_replaced)
            {
                _replaced = true;
                DescriptionValue = SR.GetString(base.Description);
            }

            return base.Description;
        }
    }

    public override object TypeId => typeof(DescriptionAttribute);
}
