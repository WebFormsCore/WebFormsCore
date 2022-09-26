using System;
using System.Buffers;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Serializer;

namespace WebFormsCore;

public static class ViewStateWriterExtensions
{
    public static void Write<T>(ViewStateWriter writer, T value)
        where T : IViewStateObject
    {
        value.WriteViewState(ref writer);
    }
}

public ref struct ViewStateWriter
{
    private readonly IServiceProvider _provider;
    private IMemoryOwner<byte> _owner;
    private Span<byte> _span;
    private int _length;
    private bool _isDisposed;

    public ViewStateWriter(IServiceProvider provider)
    {
        _length = 0;
        _provider = provider;
        _owner = MemoryPool<byte>.Shared.Rent(1024);
        _span = _owner.Memory.Span;
        _isDisposed = false;
    }

    public Span<byte> Span => _owner.Memory.Span.Slice(0, _length);

    public void Write<T>(T value)
    {
        var serializer = _provider.GetService<IViewStateSerializer<T>>();

        int length;

        if (serializer != null)
        {
            while (!serializer.TryWrite(value, _span, out length))
            {
                Grow();
            }
        }
        else
        {
            var objSerializer = _provider
                .GetServices<IViewStateSerializer>()
                .FirstOrDefault(i => i.CanSerialize(typeof(T)));

            if (objSerializer == null)
            {
                throw new InvalidOperationException($"No serializer found for type {typeof(T).FullName}");
            }

            while (!objSerializer.TryWrite(value, _span, out length))
            {
                Grow();
            }
        }

        _length += length;
        _span = _span.Slice(length);
    }
    
    private void Grow()
    {
        var current = _owner;
        var newMemory = MemoryPool<byte>.Shared.Rent(_owner.Memory.Length * 2);

        current.Memory.Slice(0, _length).CopyTo(newMemory.Memory);
        current.Dispose();

        _owner = newMemory;
        _span = newMemory.Memory.Span.Slice(_length);
    }

    internal void Dispose()
    {
        if (!_isDisposed)
        {
            _owner.Dispose();
        }

        _isDisposed = true;
    }
}
