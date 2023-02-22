using System;

namespace WebFormsCore.UI;

public class ViewStateException : Exception
{
    public ViewStateException(string? message) : base(message)
    {
    }

    public ViewStateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
