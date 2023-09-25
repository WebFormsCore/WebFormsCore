using System;

namespace WebFormsCore.UI.WebControls;

/// <summary>
/// Provides data for the <see langword='Command'/> event.
/// </summary>
public class CommandEventArgs : EventArgs
{
    /// <summary>
    /// <para>Initializes a new instance of the <see cref='CommandEventArgs'/> class with another <see cref='CommandEventArgs'/>.</para>
    /// </summary>
    public CommandEventArgs(CommandEventArgs e)
        : this(e.CommandName, e.CommandArgument)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='CommandEventArgs'/> class with the specified command name
    /// and argument.
    /// </summary>
    public CommandEventArgs(string? commandName, object? argument)
    {
        CommandName = commandName;
        CommandArgument = argument;
    }


    /// <summary>
    /// Gets the name of the command. This property is read-only.
    /// </summary>
    public string? CommandName { get; }


    /// <summary>
    /// Gets the argument for the command. This property is read-only.
    /// </summary>
    public object? CommandArgument { get; }
}
