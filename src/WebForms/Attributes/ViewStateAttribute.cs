using System;

namespace WebFormsCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ViewStateAttribute : Attribute
    {
    }
}
