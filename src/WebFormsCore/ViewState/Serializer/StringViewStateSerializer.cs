using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WebFormsCore.Serializer;

public class StringViewStateSerializer : ViewStateSerializer<string>
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    public override bool TryWrite(string? value, Span<byte> span, out int length)
    {
        if (value is null)
        {
            span[0] = 0;
            length = 1;
            return true;
        }
        
        if (value is "")
        {
            span[0] = 1;
            length = 1;
            return true;
        }

        var byteCount = Encoding.GetByteCount(value);
        length = byteCount + 2;

        if (span.Length < length)
        {
            return false;
        }

        var size = (ushort) (byteCount + 2);
        MemoryMarshal.Write(span.Slice(0, 2), ref size);
        
        Encoding.GetBytes(value, span.Slice(2));
        
        return true;
    }

    public override string? Read(ReadOnlySpan<byte> span, out int length)
    {
        if (span[0] == 0)
        {
            length = 1;
            return null;
        }

        if (span[0] == 1)
        {
            length = 1;
            return "";
        }

        var byteCount = MemoryMarshal.Read<ushort>(span.Slice(0, 2)) - 2;

        length = byteCount + 2;
        return Encoding.GetString(span.Slice(2, byteCount));
    }
}
