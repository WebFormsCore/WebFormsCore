using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;
using WebFormsCore.UI;

namespace WebFormsCore.Serializer;

public class StringViewStateSerializer(IOptions<ViewStateOptions>? options = null) : ViewStateSerializer<string>, IViewStateSpanSerializer<char>
{
    private readonly IOptions<ViewStateOptions> _options = options ?? Options.Create(new ViewStateOptions());

    public override void Write(Type type, ref ViewStateWriter writer, string? value, string? defaultValue)
    {
        if (value is null)
        {
            var size = ushort.MaxValue;
            MemoryMarshal.Write(writer.GetUnsafeSpan(sizeof(ushort)), in size);
            writer.Skip(sizeof(ushort));
            return;
        }

        Write(ref writer, value.AsSpan());
    }

    public void Write(ref ViewStateWriter writer, scoped ReadOnlySpan<char> value)
    {
        var sizeSpan = writer.Reserve(sizeof(ushort));
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        if (maxByteCount > 4096) maxByteCount = Encoding.UTF8.GetByteCount(value);
        var span = writer.GetUnsafeSpan(maxByteCount);
        var byteCount = Encoding.UTF8.GetBytes(value, span);
        var size = (ushort)byteCount;

        if (size > _options.Value.MaxStringLength)
        {
            throw new ViewStateException("String is too long");
        }

        MemoryMarshal.Write(writer.GetUnsafeSpan(sizeSpan), in size);
        writer.Skip(byteCount);
    }

    public override string? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, string? defaultValue)
    {
        var sizeSpan = reader.ReadBytes(sizeof(ushort));
        var size = MemoryMarshal.Read<ushort>(sizeSpan);

        if (size == ushort.MaxValue)
        {
            return null;
        }

        if (size > _options.Value.MaxStringLength)
        {
            throw new ViewStateException("String is too long");
        }

        var span = reader.ReadBytes(size);

        return Encoding.UTF8.GetString(span);
    }

    public int Read(ref ViewStateReader reader, scoped Span<char> value)
    {
        var sizeSpan = reader.ReadBytes(sizeof(ushort));
        var size = MemoryMarshal.Read<ushort>(sizeSpan);

        if (size == ushort.MaxValue)
        {
            return 0;
        }

        if (size > _options.Value.MaxStringLength)
        {
            throw new ViewStateException("String is too long");
        }

        var span = reader.ReadBytes(size);
        return Encoding.UTF8.GetChars(span, value);
    }

    public override bool StoreInViewState(Type type, string? value, string? defaultValue)
    {
        return !string.Equals(value, defaultValue, StringComparison.Ordinal);
    }

    public override void TrackViewState(Type type, string? value, ViewStateProvider provider)
    {
    }
}
