using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public sealed class CssStyleCollection : IEquatable<CssStyleCollection>, IViewStateObject
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

    private readonly IDictionary<string, string?>? _state;
    private string? _style;

    private readonly HashSet<string> _trackedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<HtmlTextWriterStyle> _trackedIntKeys = new();

    private Dictionary<string, string?>? _table;
    private Dictionary<HtmlTextWriterStyle, string?>? _intTable;


    internal CssStyleCollection() : this(null)
    {
    }

    /*
     * Constructs an CssStyleCollection given a StateBag.
     */
    internal CssStyleCollection(IDictionary<string, string?>? state)
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
            if (string.IsNullOrEmpty(key)) return null;

            _table ??= ParseString();

            if (_table.TryGetValue(key, out var value))
            {
                return value;
            }

            var styleKey = CssTextWriter.GetStyleKey(key);

            if (styleKey != (HtmlTextWriterStyle)(-1))
            {
                return this[styleKey];
            }

            return null;
        }
        set => Add(key, value);
    }


    /// <devdoc>
    /// Gets or sets the specified known CSS value.
    /// </devdoc>
    public string? this[HtmlTextWriterStyle key]
    {
        get
        {
            _table ??= ParseString();
            return _intTable != null && _intTable.TryGetValue(key, out var value) ? value : null;
        }
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

            if (!_state.TryGetValue("style", out var value))
            {
                value = BuildString();
                _state["style"] = value;
            }

            return value;
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
            _intTable = null;
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

        var styleKey = CssTextWriter.GetStyleKey(key);

        if (styleKey != (HtmlTextWriterStyle)(-1))
        {
            Add(styleKey, value);
            return;
        }

        _table ??= ParseString();
        _table[key] = value;
        _trackedKeys.Add(key);

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
        _trackedIntKeys.Add(key);

        var name = CssTextWriter.GetStyleName(key);
        if (name.Length != 0)
        {
            // Remove from the other table to avoid duplicates.
            _table ??= ParseString();
            _table.Remove(name);
            _trackedKeys.Remove(name);
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
        if (string.IsNullOrEmpty(key)) return;

        _table ??= ParseString();

        var removed = _table.Remove(key);
        if (removed)
        {
            _trackedKeys.Add(key);
        }

        var styleKey = CssTextWriter.GetStyleKey(key);
        if (styleKey != (HtmlTextWriterStyle)(-1))
        {
            if (_intTable != null && _intTable.Remove(styleKey))
            {
                _trackedIntKeys.Add(styleKey);
                removed = true;
            }
        }

        if (removed)
        {
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
        _trackedIntKeys.Add(key);

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

        var s = _state == null ? _style : _state["style"];
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
                var styleName = name[i].ToString().Trim();
                var styleValue = values[i].ToString().Trim();

                var styleKey = CssTextWriter.GetStyleKey(styleName);

                if (styleKey != (HtmlTextWriterStyle)(-1))
                {
                    _intTable ??= new Dictionary<HtmlTextWriterStyle, string?>();
                    _intTable[styleKey] = styleValue;
                }
                else
                {
                    table[styleName] = styleValue;
                }
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
        _table ??= ParseString();

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

    public async Task RenderAsync(HtmlTextWriter writer)
    {
        _table ??= ParseString();

        if (_table is {Count: 0} && (_intTable is null or { Count: 0 }))
        {
            return;
        }

        await writer.WriteAsync(" style=\"");

        if (_table is { Count: > 0 })
        {
            foreach (var entry in _table)
            {
                await writer.WriteAsync(entry.Key);
                await writer.WriteAsync(": ");
                await writer.WriteAsync(entry.Value);
                await writer.WriteAsync(';');
            }
        }

        if (_intTable is { Count: > 0 })
        {
            foreach (var entry in _intTable)
            {
                await writer.WriteAsync(CssTextWriter.GetStyleName(entry.Key));
                await writer.WriteAsync(": ");
                await writer.WriteAsync(entry.Value);
                await writer.WriteAsync(';');
            }
        }

        await writer.WriteAsync('"');
    }

    public void AddAttributes(HtmlTextWriter writer)
    {
        if (_table is not { Count: > 0 } && _intTable is not { Count: > 0 })
        {
            return;
        }

        var sb = new StringBuilder();

        if (_table is { Count: > 0 })
        {
            foreach (var entry in _table)
            {
                sb.Append(entry.Key);
                sb.Append(": ");
                sb.Append(entry.Value);
                sb.Append(';');
            }
        }

        if (_intTable is { Count: > 0 })
        {
            foreach (var entry in _intTable)
            {
                sb.Append(CssTextWriter.GetStyleName(entry.Key));
                sb.Append(": ");
                sb.Append(entry.Value);
                sb.Append(';');
            }
        }

        writer.AddAttribute("style", sb.ToString());
    }

    public bool Equals(CssStyleCollection? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        var leftTable = _table ?? ParseString();
        var rightTable = other._table ?? other.ParseString();

        if (leftTable.Count != rightTable.Count)
        {
            return false;
        }

        foreach (var (key, value) in leftTable)
        {
            if (!rightTable.TryGetValue(key, out var otherValue) || value != otherValue)
            {
                return false;
            }
        }

        var leftIntCount = _intTable?.Count ?? 0;
        var rightIntCount = other._intTable?.Count ?? 0;

        if (leftIntCount != rightIntCount)
        {
            return false;
        }

        if (leftIntCount > 0)
        {
            foreach (var (key, value) in _intTable!)
            {
                if (!other._intTable!.TryGetValue(key, out var otherValue) || value != otherValue)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CssStyleCollection other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;

        var table = _table ?? ParseString();
        foreach (var (key, value) in table)
        {
            var keyHash = StringComparer.OrdinalIgnoreCase.GetHashCode(key);
            var valueHash = value?.GetHashCode() ?? 0;
            hashCode ^= HashCode.Combine(keyHash, valueHash);
        }

        if (_intTable != null)
        {
            foreach (var (key, value) in _intTable)
            {
                var keyHash = key.GetHashCode();
                var valueHash = value?.GetHashCode() ?? 0;
                hashCode ^= HashCode.Combine(keyHash, valueHash);
            }
        }

        return hashCode;
    }

    public static bool operator ==(CssStyleCollection? left, CssStyleCollection? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CssStyleCollection? left, CssStyleCollection? right)
    {
        return !Equals(left, right);
    }

    internal bool WriteToViewState => _trackedKeys.Count > 0 || _trackedIntKeys.Count > 0;

    internal void TrackViewState(ViewStateProvider provider)
    {
        _trackedKeys.Clear();
        _trackedIntKeys.Clear();
    }

    internal void WriteViewState(ref ViewStateWriter writer)
    {
        writer.Write((ushort) (_trackedKeys?.Count ?? 0));

        if (_trackedKeys is not null)
        {
            foreach (var key in _trackedKeys)
            {
                writer.Write(key);
                writer.Write(_table != null && _table.TryGetValue(key, out var value) ? value : null);
            }
        }

        writer.Write((ushort) (_trackedIntKeys?.Count ?? 0));

        if (_trackedIntKeys is not null)
        {
            foreach (var key in _trackedIntKeys)
            {
                writer.Write((byte)key);
                writer.Write(_intTable != null && _intTable.TryGetValue(key, out var value) ? value : null);
            }
        }
    }

    internal void ReadViewState(ref ViewStateReader reader)
    {
        _table ??= ParseString();

        var count = reader.Read<ushort>();

        for (var i = 0; i < count; i++)
        {
            var key = reader.Read<string>();

            if (key is null) continue;

            var value = reader.Read<string>();

            if (value is not null)
            {
                _table[key] = value;
            }
            else
            {
                _table.Remove(key);
            }

            _trackedKeys.Add(key);
        }

        count = reader.Read<ushort>();

        if (count > 0)
        {
            _intTable ??= new Dictionary<HtmlTextWriterStyle, string?>();

            for (var i = 0; i < count; i++)
            {
                var key = (HtmlTextWriterStyle)reader.ReadByte();
                var value = reader.Read<string>();

                if (value is not null)
                {
                    _intTable[key] = value;
                }
                else
                {
                    _intTable.Remove(key);
                }

                _trackedIntKeys.Add(key);
            }
        }
    }

    bool IViewStateObject.WriteToViewState => WriteToViewState;
    void IViewStateObject.TrackViewState(ViewStateProvider provider) => TrackViewState(provider);
    void IViewStateObject.ReadViewState(ref ViewStateReader reader) => ReadViewState(ref reader);
    void IViewStateObject.WriteViewState(ref ViewStateWriter writer) => WriteViewState(ref writer);
}
