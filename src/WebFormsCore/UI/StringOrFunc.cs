using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI;

public readonly struct StringOrFunc
{
    private readonly string? _value;
    private readonly Func<string>? _func;

    public StringOrFunc(string? value)
    {
        _value = value;
        _func = null;
    }

    public StringOrFunc(Func<string> func)
    {
        _func = func;
        _value = null;
    }

    public override string? ToString()
    {
        return _func != null ? _func() : _value;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is StringOrFunc other &&
               Equals(_value, other._value) &&
               Equals(_func, other._func);
    }

    public override int GetHashCode()
    {
        return _value?.GetHashCode() ?? _func?.GetHashCode() ?? 0;
    }

    public bool IsEmpty => _value == null && _func == null;

    public static implicit operator StringOrFunc(string? value) => new(value);
    public static implicit operator StringOrFunc(Func<string> func) => new(func);

    public static implicit operator string?(StringOrFunc stringOrFunc) => stringOrFunc.ToString();
}
