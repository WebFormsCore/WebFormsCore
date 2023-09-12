using System;

namespace WebFormsCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ViewStateAttribute : Attribute
    {
        public ViewStateAttribute(string? validateProperty = null)
        {
            ValidateProperty = validateProperty;
        }

        public string? ValidateProperty { get; }

        public bool WriteAlways { get; set; }
    }
}
