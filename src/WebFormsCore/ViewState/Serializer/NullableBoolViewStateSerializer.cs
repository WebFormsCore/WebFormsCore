using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.Serializer;

public class NullableBoolViewStateSerializer : ViewStateSerializer<bool?>
{
    public override void Write(Type type, ref ViewStateWriter writer, bool? value, bool? defaultValue)
    {
        if (!value.HasValue)
        {
            writer.WriteByte(2);
        }
        else if (value.Value)
        {
            writer.WriteByte(1);
        }
        else
        {
            writer.WriteByte(0);
        }
    }

    public override bool? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, bool? defaultValue)
    {
        var value = reader.ReadByte();

        return value switch
        {
            0 => false,
            1 => true,
            _ => null
        };
    }

    public override bool StoreInViewState(Type type, bool? value, bool? defaultValue)
    {
        return value.HasValue;
    }

    public override void TrackViewState(Type type, bool? value, ViewStateProvider provider)
    {
    }
}
