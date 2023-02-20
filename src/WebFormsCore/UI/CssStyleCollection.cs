using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WebFormsCore.UI;

public sealed class CssStyleCollection
{
    private static readonly Regex StyleAttribRegex = new(
        "\\G(\\s*(;\\s*)*" + // match leading semicolons and spaces
        "(?<stylename>[^:]+?)" + // match stylename - chars up to the semicolon
        "\\s*:\\s*" + // spaces, then the colon, then more spaces
        "(?<styleval>[^;]*)" + // now match styleval
        ")*\\s*(;\\s*)*$", // match a trailing semicolon and trailing spaces
        RegexOptions.Singleline |
        RegexOptions.Multiline |
        RegexOptions.ExplicitCapture);

    private readonly StateBag? _state;
    private string? _style;

    private Dictionary<string, string?>? _table;
    private Dictionary<HtmlTextWriterStyle, string?>? _intTable;


    internal CssStyleCollection() : this(null)
    {
    }

    /*
     * Constructs an CssStyleCollection given a StateBag.
     */
    internal CssStyleCollection(StateBag? state)
    {
        _state = state;
    }

    /*
     * Automatically adds new keys.
     */

    /// <devdoc>
    ///    <para>
    ///       Gets or sets a specified CSS value.
    ///    </para>
    /// </devdoc>
    public string? this[string key]
    {
        get
        {
            _table ??= ParseString();
            var value = _table[key];

            if (value == null)
            {
                var style = CssTextWriter.GetStyleKey(key);
                if (style != (HtmlTextWriterStyle)(-1))
                {
                    value = this[style];
                }
            }

            return value;
        }
        set => Add(key, value);
    }


    /// <devdoc>
    /// Gets or sets the specified known CSS value.
    /// </devdoc>
    public string? this[HtmlTextWriterStyle key]
    {
        get => _intTable?[key];
        set => Add(key, value);
    }

    /*
     * Returns a collection of keys.
     */

    /// <devdoc>
    ///    <para>
    ///       Gets a collection of keys to all the styles in the
    ///    <see langword='CssStyleCollection'/>.
    ///    </para>
    /// </devdoc>
    public ICollection Keys
    {
        get
        {
            _table ??= ParseString();

            if (_intTable != null)
            {
                // combine the keys into a single table. Note that to preserve existing
                // behavior, we convert enum values into strings to maintain a homogeneous collection.

                var keys = new string[_table.Count + _intTable.Count];
                var i = 0;

                foreach (var s in _table.Keys)
                {
                    keys[i] = s;
                    i++;
                }

                foreach (var style in _intTable.Keys)
                {
                    keys[i] = CssTextWriter.GetStyleName(style);
                    i++;
                }

                return keys;
            }

            return _table.Keys;
        }
    }


    /// <devdoc>
    ///    <para>
    ///       Gets the number of items in the <see langword='CssStyleCollection'/>.
    ///    </para>
    /// </devdoc>
    public int Count
    {
        get
        {
            _table ??= ParseString();
            return _table.Count + (_intTable?.Count ?? 0);
        }
    }


    public string? Value
    {
        get
        {
            if (_state == null)
            {
                return _style ??= BuildString();
            }

            return (string?)_state["style"];
        }
        set
        {
            if (_state == null)
            {
                _style = value;
            }
            else
            {
                _state["style"] = value;
            }

            _table = null;
        }
    }


    /// <devdoc>
    ///    <para>
    ///       Adds a style to the CssStyleCollection.
    ///    </para>
    /// </devdoc>
    public void Add(string key, string? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _table ??= ParseString();
        _table[key] = value;

        if (_intTable != null)
        {
            // Remove from the other table to avoid duplicates.
            var style = CssTextWriter.GetStyleKey(key);
            if (style != (HtmlTextWriterStyle)(-1))
            {
                _intTable.Remove(style);
            }
        }

        if (_state != null)
        {
            // keep style attribute synchronized
            _state["style"] = BuildString();
        }

        _style = null;
    }


    public void Add(HtmlTextWriterStyle key, string? value)
    {
        _intTable ??= new Dictionary<HtmlTextWriterStyle, string?>();
        _intTable[key] = value;

        var name = CssTextWriter.GetStyleName(key);
        if (name.Length != 0)
        {
            // Remove from the other table to avoid duplicates.
            _table ??= ParseString();
            _table.Remove(name);
        }

        if (_state != null)
        {
            // keep style attribute synchronized
            _state["style"] = BuildString();
        }

        _style = null;
    }


    /// <devdoc>
    ///    <para>
    ///       Removes a style from the <see langword='CssStyleCollection'/>.
    ///    </para>
    /// </devdoc>
    public void Remove(string key)
    {
        _table ??= ParseString();

        if (_table[key] != null)
        {
            _table.Remove(key);

            if (_state != null)
            {
                // keep style attribute synchronized
                _state["style"] = BuildString();
            }

            _style = null;
        }
    }


    public void Remove(HtmlTextWriterStyle key)
    {
        if (_intTable == null)
        {
            return;
        }

        _intTable.Remove(key);

        if (_state != null)
        {
            // keep style attribute synchronized
            _state["style"] = BuildString();
        }

        _style = null;
    }


    /// <devdoc>
    ///    <para>
    ///       Removes all styles from the <see langword='CssStyleCollection'/>.
    ///    </para>
    /// </devdoc>
    public void Clear()
    {
        _table = null;
        _intTable = null;

        if (_state != null)
        {
            _state.Remove("style");
        }

        _style = null;
    }

    /*  BuildString
     *  Form the style string from data contained in the
     *  hash table
     */
    private string? BuildString()
    {
        // if the tables are null, there is nothing to build
        if ((_table == null || _table.Count == 0) &&
            (_intTable == null || _intTable.Count == 0))
        {
            return null;
        }

        var sw = new StringWriter();
        var writer = new CssTextWriter(sw);

        Render(writer);
        return sw.ToString();
    }

    /*  ParseString
     *  Parse the style string and fill the hash table with
     *  corresponding values.
     */
    private Dictionary<string, string?> ParseString()
    {
        // create a case-insensitive dictionary
        var table = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var s = _state == null ? _style : (string?)_state["style"];
        if (s == null)
        {
            return table;
        }

        foreach (Match match in StyleAttribRegex.Matches(s))
        {
            var name = match.Groups["stylename"].Captures;
            var values = match.Groups["styleval"].Captures;

            for (var i = 0; i < name.Count; i++)
            {
                var styleName = name[i].ToString();
                var styleValue = values[i].ToString();

                table[styleName] = styleValue;
            }
        }

        return table;
    }


    /// <devdoc>
    /// Render out the attribute collection into a CSS TextWriter. This
    /// effectively renders the value of an inline style attribute.
    /// </devdoc>
    internal void Render(CssTextWriter writer)
    {
        if (_table is { Count: > 0 })
        {
            foreach (var entry in _table)
            {
                writer.WriteAttribute(entry.Key, entry.Value);
            }
        }

        if (_intTable is { Count: > 0 })
        {
            foreach (var entry in _intTable)
            {
                writer.WriteAttribute(entry.Key, entry.Value);
            }
        }
    }

    /// <devdoc>
    /// Render out the attribute collection into a CSS TextWriter. This
    /// effectively renders the value of an inline style attribute.
    /// Used by a Style object to render out its CSS attributes into an HtmlTextWriter.
    /// </devdoc>
    internal void Render(HtmlTextWriter writer)
    {
        if (_table is { Count: > 0 })
        {
            foreach (var entry in _table)
            {
                writer.AddStyleAttribute(entry.Key, entry.Value);
            }
        }

        if (_intTable is { Count: > 0 })
        {
            foreach (var entry in _intTable)
            {
                writer.AddStyleAttribute(entry.Key, entry.Value);
            }
        }
    }
}
