using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WebFormsCore.Serializer;

public class StringViewStateSerializer : ViewStateSerializer<string>, IViewStateSpanSerializer<char>
{
    public override void Write(Type type, ref ViewStateWriter writer, string? value, string? defaultValue)
    {
        var sizeSpan = writer.Reserve(sizeof(ushort));
        ushort size;

        if (value is null)
        {
            size = ushort.MaxValue;
            MemoryMarshal.Write(writer.GetUnsafeSpan(sizeSpan), ref size);
            return;
        }

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        if (maxByteCount > 4096) maxByteCount = Encoding.UTF8.GetByteCount(value);
        var span = writer.GetUnsafeSpan(maxByteCount);
        var byteCount = Encoding.UTF8.GetBytes(value, span);

        size = (ushort)byteCount;

        MemoryMarshal.Write(writer.GetUnsafeSpan(sizeSpan), ref size);
        writer.Skip(byteCount);
    }

    public void Write(ref ViewStateWriter writer, scoped ReadOnlySpan<char> value)
    {
        var sizeSpan = writer.Reserve(sizeof(ushort));
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        if (maxByteCount > 4096) maxByteCount = Encoding.UTF8.GetByteCount(value);
        var span = writer.GetUnsafeSpan(maxByteCount);
        var byteCount = Encoding.UTF8.GetBytes(value, span);
        var size = (ushort)byteCount;

        MemoryMarshal.Write(writer.GetUnsafeSpan(sizeSpan), ref size);
        writer.Skip(byteCount);
    }

    public override string? Read(Type type, ref ViewStateReader reader, string? defaultValue)
    {
        var sizeSpan = reader.ReadBytes(sizeof(ushort));
        var size = MemoryMarshal.Read<ushort>(sizeSpan);

        if (size == ushort.MaxValue)
        {
            return null;
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
