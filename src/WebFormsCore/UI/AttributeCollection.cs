using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public sealed class AttributeCollection : IDictionary<string, string?>, IViewStateObject, IAttributeAccessor
{
    private static readonly Dictionary<byte, string> IdToKey;
    private static readonly Dictionary<string, byte> KeyToId;

    static AttributeCollection()
    {
        IdToKey = new Dictionary<byte, string>
        {
            [0] = "accept",
            [1] = "accept-charset",
            [2] = "accesskey",
            [3] = "action",
            [4] = "align",
            [5] = "allow",
            [6] = "alt",
            [7] = "async",
            [8] = "autocapitalize",
            [9] = "autocomplete",
            [10] = "autofocus",
            [11] = "autoplay",
            [12] = "background",
            [13] = "bgcolor",
            [14] = "border",
            [15] = "buffered",
            [16] = "capture",
            [17] = "charset",
            [18] = "checked",
            [19] = "cite",
            [20] = "class",
            [21] = "color",
            [22] = "cols",
            [23] = "colspan",
            [24] = "content",
            [25] = "contenteditable",
            [26] = "contextmenu",
            [27] = "controls",
            [28] = "coords",
            [29] = "crossorigin",
            [30] = "csp",
            [31] = "data",
            [32] = "datetime",
            [33] = "decoding",
            [34] = "default",
            [35] = "defer",
            [36] = "dir",
            [37] = "dirname",
            [38] = "disabled",
            [39] = "download",
            [40] = "draggable",
            [41] = "enctype",
            [42] = "enterkeyhint",
            [43] = "for",
            [44] = "form",
            [45] = "formaction",
            [46] = "formenctype",
            [47] = "formmethod",
            [48] = "formnovalidate",
            [49] = "formtarget",
            [50] = "headers",
            [51] = "height",
            [52] = "hidden",
            [53] = "high",
            [54] = "href",
            [55] = "hreflang",
            [56] = "http-equiv",
            [57] = "id",
            [58] = "integrity",
            [59] = "intrinsicsize",
            [60] = "inputmode",
            [61] = "ismap",
            [62] = "itemprop",
            [63] = "kind",
            [64] = "label",
            [65] = "lang",
            [66] = "language",
            [67] = "loading",
            [68] = "list",
            [69] = "loop",
            [70] = "low",
            [71] = "manifest",
            [72] = "max",
            [73] = "maxlength",
            [74] = "minlength",
            [75] = "media",
            [76] = "method",
            [77] = "min",
            [78] = "multiple",
            [79] = "muted",
            [80] = "name",
            [81] = "novalidate",
            [82] = "open",
            [83] = "optimum",
            [84] = "pattern",
            [85] = "ping",
            [86] = "placeholder",
            [87] = "playsinline",
            [88] = "poster",
            [89] = "preload",
            [90] = "readonly",
            [91] = "referrerpolicy",
            [92] = "rel",
            [93] = "required",
            [94] = "reversed",
            [95] = "role",
            [96] = "rows",
            [97] = "rowspan",
            [98] = "sandbox",
            [99] = "scope",
            [100] = "scoped",
            [101] = "selected",
            [102] = "shape",
            [103] = "size",
            [104] = "sizes",
            [105] = "slot",
            [106] = "span",
            [107] = "spellcheck",
            [108] = "src",
            [109] = "srcdoc",
            [110] = "srclang",
            [111] = "srcset",
            [112] = "start",
            [113] = "step",
            [114] = "style",
            [115] = "summary",
            [116] = "tabindex",
            [117] = "target",
            [118] = "title",
            [119] = "translate",
            [120] = "type",
            [121] = "usemap",
            [122] = "value",
            [123] = "width",
            [124] = "wrap"
        };
        KeyToId = IdToKey.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);
    }

    private readonly Dictionary<string, string?> _bag = new();
    private CssStyleCollection? _styleColl;
    private readonly HashSet<string> _trackedKeys = new(StringComparer.OrdinalIgnoreCase);

    public string? CssClass
    {
        get => this["class"];
        set => this["class"] = value;
    }

    public bool TryGetValue(string key, out string? value)
    {
        return _bag.TryGetValue(key, out value);
    }

    public string? this[string key]
    {
        get
        {
            if (_styleColl != null && key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
            {
                return _styleColl.Value;
            }

            if (_bag.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
        set => Add(key, value);
    }

    ICollection<string> IDictionary<string, string?>.Keys => _bag.Keys;

    public ICollection<string?> Values => _bag.Values;

    public ICollection Keys => _bag.Keys;

    public int Count => _bag.Count;
    public bool IsReadOnly => false;

    public CssStyleCollection CssStyle => _styleColl ??= new CssStyleCollection();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Track(string key)
    {
        _trackedKeys.Add(key);
    }

    public void Add(string key, string? value)
    {
        Track(key);

        if (key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
        {
            CssStyle.Value = value;
        }
        else
        {
            _bag[key] = value;
        }
    }

    public bool ContainsKey(string key)
    {
        return _bag.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        if (_styleColl != null && key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
        {
            if (_styleColl.Count == 0)
            {
                return false;
            }

            _styleColl.Clear();
            return true;
        }

        Track(key);
        return _bag.Remove(key);
    }

    public void Clear()
    {
        foreach (var key in _bag.Keys)
        {
            Track(key);
        }

        _bag.Clear();
        _styleColl?.Clear();
    }

    public async Task RenderAsync(HtmlTextWriter writer)
    {
        foreach (var kv in _bag)
        {
            await writer.WriteAttributeAsync(kv.Key, kv.Value);
        }
    }

    public void AddAttributes(HtmlTextWriter writer)
    {
        foreach (var kv in _bag)
        {
            writer.AddAttribute(kv.Key, kv.Value);
        }
    }

    public Dictionary<string, string?>.Enumerator GetEnumerator() => _bag.GetEnumerator();

    IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => _bag.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _bag.GetEnumerator();

    #region Collection

    void ICollection<KeyValuePair<string, string?>>.Add(KeyValuePair<string, string?> item)
    {
        Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<string, string?>>.Remove(KeyValuePair<string, string?> item)
    {
        return Remove(item.Key);
    }

    bool ICollection<KeyValuePair<string, string?>>.Contains(KeyValuePair<string, string?> item)
    {
        return ContainsKey(item.Key);
    }

    void ICollection<KeyValuePair<string, string?>>.CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, string?>>)_bag).CopyTo(array, arrayIndex);
    }

    #endregion

    public void CopyTo(IAttributeAccessor destination)
    {
        foreach (var kv in _bag)
        {
            destination.SetAttribute(kv.Key, kv.Value);
        }
    }

    bool IViewStateObject.WriteToViewState => _trackedKeys.Count > 0;

    void IViewStateObject.TrackViewState(ViewStateProvider provider)
    {
        _trackedKeys.Clear();
    }

    void IViewStateObject.WriteViewState(ref ViewStateWriter writer)
    {
        writer.Write((ushort) _trackedKeys.Count);

        foreach (var key in _trackedKeys)
        {
            if (KeyToId.TryGetValue(key, out var id))
            {
                writer.Write(id);
            }
            else
            {
                writer.Write(byte.MaxValue);
                writer.Write(key);
            }

            writer.Write(_bag[key]);
        }
    }

    void IViewStateObject.ReadViewState(ref ViewStateReader reader)
    {
        var count = reader.Read<ushort>();

        for (var i = 0; i < count; i++)
        {
            var id = reader.Read<byte>();

            string? key;

            if (id == byte.MaxValue)
            {
                key = reader.Read<string>();

                if (key is null) continue;
            }
            else if (!IdToKey.TryGetValue(id, out key))
            {
                continue;
            }

            var value = reader.Read<string>();

            _bag[key] = value;
            _trackedKeys.Add(key);
        }
    }

    public string? GetAttribute(string key)
    {
        return this[key];
    }

    public void SetAttribute(string key, string? value)
    {
        this[key] = value;
    }
}
