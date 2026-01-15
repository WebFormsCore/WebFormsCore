using System;

namespace WebFormsCore;

/// <summary>
/// Exception thrown when StreamPanel is not properly configured.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public class StreamPanelConfigurationException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
}
