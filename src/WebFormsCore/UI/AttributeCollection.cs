using System;
using System.Collections;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public sealed class AttributeCollection
{
    private readonly StateBag _bag;
    private CssStyleCollection? _styleColl;

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

    public async Task RenderAsync(HtmlTextWriter writer)
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
