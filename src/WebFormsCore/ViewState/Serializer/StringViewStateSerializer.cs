using System;
using System.Text;

namespace WebFormsCore.Serializer;

public class StringViewStateSerializer : FixedLengthViewStateSerializer<string>
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    protected override bool IsDefaultValue(string value) => value is "";

    protected override string GetDefaultValue() => "";

    protected override int WriteValue(string value, Span<byte> span) => Encoding.GetBytes(value, span);

    protected override string ReadValue(ReadOnlySpan<byte> span) => Encoding.GetString(span);

    protected override bool TryGetLengthImpl(string value, out int length)
    {
        length = Encoding.GetByteCount(value);
        return true;
    }
}
