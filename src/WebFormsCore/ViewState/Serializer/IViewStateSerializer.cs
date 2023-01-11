using System;

namespace WebFormsCore.Serializer;

public interface IViewStateSerializer
{
    bool CanSerialize(Type type);

    bool TryWrite(object? value, Span<byte> span, out int length);

    object? Read(ReadOnlySpan<byte> span, out int length);
}

public interface IViewStateSerializer<T> : IViewStateSerializer
    where T : notnull
{
    bool TryWrite(T? value, Span<byte> span, out int length);

    new T? Read(ReadOnlySpan<byte> span, out int length);
}

public abstract class ViewStateSerializer<T> : IViewStateSerializer<T>
    where T : notnull
{
    public abstract bool TryWrite(T? value, Span<byte> span, out int length);

    public abstract T? Read(ReadOnlySpan<byte> span, out int length);

    public bool CanSerialize(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    bool IViewStateSerializer.TryWrite(object? value, Span<byte> span, out int length)
    {
        if (value is not T t)
        {
            throw new InvalidOperationException("Invalid type");
        }

        return TryWrite(t, span, out length);
    }

    object? IViewStateSerializer.Read(ReadOnlySpan<byte> span, out int length)
    {
        return Read(span, out length);
    }
}
