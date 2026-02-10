using System;
using System.Diagnostics.CodeAnalysis;
using WebFormsCore.UI;

namespace WebFormsCore.Serializer;

/// <summary>
/// Serializes <see cref="StringOrFunc"/> by converting to/from <see cref="string"/> via <see cref="StringViewStateSerializer"/>.
/// </summary>
public class StringOrFuncViewStateSerializer(StringViewStateSerializer stringSerializer) : ViewStateSerializer<StringOrFunc>
{
    public override void Write(Type type, ref ViewStateWriter writer, StringOrFunc value, StringOrFunc defaultValue)
    {
        stringSerializer.Write(typeof(string), ref writer, value.ToString(), defaultValue.ToString());
    }

    public override StringOrFunc Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, StringOrFunc defaultValue)
    {
        var result = stringSerializer.Read(typeof(string), ref reader, defaultValue.ToString());
        return result is not null ? new StringOrFunc(result) : default;
    }

    public override bool StoreInViewState(Type type, StringOrFunc value, StringOrFunc defaultValue)
    {
        return !string.Equals(value.ToString(), defaultValue.ToString(), StringComparison.Ordinal);
    }

    public override void TrackViewState(Type type, StringOrFunc value, ViewStateProvider provider)
    {
    }
}
