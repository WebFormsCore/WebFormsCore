using System;
using JetBrains.Annotations;

namespace WebFormsCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ViewStateAttribute : Attribute
    {
        public ViewStateAttribute([LanguageInjection("C#")] string? validateExpression = null)
        {
            ValidateExpression = validateExpression;
        }

        /// <summary>
        /// Expression to validate if the value should be written to the view state.
        /// </summary>
        public string? ValidateExpression { get; }

        /// <summary>
        /// <c>true</c> to always write the value to the view state, even if it is the same as the default value.
        /// </summary>
        public bool WriteAlways { get; set; }
    }
}
