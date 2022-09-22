using System.Collections;
using System.Threading.Tasks;

namespace System.Web.UI;

/*
* The AttributeCollection represents Attributes on an Html control.
*/

/// <devdoc>
///    <para>
///       The <see langword='AttributeCollection'/> class provides object-model access
///       to all attributes declared on an HTML server control element.
///    </para>
/// </devdoc>
public sealed class AttributeCollection
{
    private readonly StateBag _bag;
    private CssStyleCollection? _styleColl;

    /*
     *      Constructs an AttributeCollection given a StateBag.
     */

    /// <devdoc>
    /// </devdoc>
    public AttributeCollection(StateBag bag)
    {
        _bag = bag;
    }

    public string? this[string key]
    {
        get
        {
            if (_styleColl != null && key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
            {
                return _styleColl.Value;
            }

            return _bag[key] as string;
        }
        set => Add(key, value);
    }

    public ICollection Keys => ((IDictionary)_bag).Keys;

    public int Count => _bag.Count;

    public CssStyleCollection CssStyle => _styleColl ??= new CssStyleCollection(_bag);

    public void Add(string key, string? value)
    {
        if (_styleColl != null && key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
        {
            _styleColl.Value = value;
        }
        else
        {
            _bag[key] = value;
        }
    }
    
    public void Remove(string key)
    {
        if (_styleColl != null && key.StartsWith("style", StringComparison.OrdinalIgnoreCase))
        {
            _styleColl.Clear();
        }
        else
        {
            _bag.Remove(key);
        }
    }

    public void Clear()
    {
        _bag.Clear();
        _styleColl?.Clear();
    }

    public async ValueTask RenderAsync(HtmlTextWriter writer)
    {
        foreach (var kv in _bag)
        {
            await writer.WriteAttributeAsync(kv.Key, kv.Value?.ToString());
        }
    }

    public void AddAttributes(HtmlTextWriter writer)
    {
        foreach (var kv in _bag)
        {
            writer.AddAttribute(kv.Key, kv.Value?.ToString());
        }
    }
}
