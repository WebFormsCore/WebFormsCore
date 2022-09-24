using System;
using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Serializer;

namespace WebFormsCore;

public static class ViewStateReaderExtensions
{
    public static void Read<T>(ViewStateReader reader)
        where T : IViewStateObject
    {
        var value = ActivatorUtilities.CreateInstance<T>(reader.Provider);
        value.ReadViewState(ref reader);
    }
}

public ref struct ViewStateReader
{
    private readonly ViewStateReaderOwner _owner;
    internal readonly IServiceProvider Provider;
    private ReadOnlySpan<byte> _span;
    private int _offset;

    internal ViewStateReader(ReadOnlySpan<byte> span, IServiceProvider provider, ViewStateReaderOwner owner)
    {
        _offset = owner.Offset;
        _span = span;
        Provider = provider;
        _owner = owner;
    }

    public T Read<T>()
    {
        var serializer = Provider.GetRequiredService<IViewStateSerializer<T>>();
        var value = serializer.Read(_span, out var length);

        _span = _span.Slice(length);
        _offset += length;
        return value;
    }

    public void Dispose()
    {
        _owner.Offset = _offset;
    }
}

internal sealed class ViewStateReaderOwner : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IMemoryOwner<byte> _memory;

    public ViewStateReaderOwner(IMemoryOwner<byte> memory, IServiceProvider provider, int offset)
    {
        Offset = offset;
        _memory = memory;
        _provider = provider;
    }

    public int Offset { get; set; }

    public ViewStateReader CreateReader()
    {
        return new ViewStateReader(_memory.Memory.Span.Slice(Offset), _provider, this);
    }

    public void Dispose()
    {
        _memory?.Dispose();
    }
}
