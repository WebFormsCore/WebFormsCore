using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Serializer;

namespace WebFormsCore;

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

    public Span<byte> RemainingSpan => _span;

    public Memory<byte> Memory => _owner.Memory.Slice(0, _length);

    public int Length => _length;

    public bool StoreInViewState(Type type, object? value, object? defaultValue)
    {
        var objSerializer = _provider
            .GetServices<IViewStateSerializer>()
            .FirstOrDefault(i => i.CanSerialize(type));

        objSerializer ??= _provider.GetRequiredService<IDefaultViewStateSerializer>();

        return objSerializer.StoreInViewState(type, value, defaultValue);
    }

    public bool StoreInViewState<T>(T? value, T? defaultValue)
        where T : notnull
    {
        var serializer = _provider.GetService<IViewStateSerializer<T>>();

        if (serializer != null)
        {
            return serializer.StoreInViewState(typeof(T), value, defaultValue);
        }

        return StoreInViewState(typeof(T), value, defaultValue);
    }

    public void Write(Type type, object? value, object? defaultValue = default)
    {
        var objSerializer = _provider
            .GetServices<IViewStateSerializer>()
            .FirstOrDefault(i => i.CanSerialize(type));

        objSerializer ??= _provider.GetRequiredService<IDefaultViewStateSerializer>();

        if (objSerializer == null)
        {
            throw new InvalidOperationException($"No serializer found for type {type.FullName}");
        }

        objSerializer.Write(type, ref this, value, defaultValue);
    }

    public void Write<T>(T? value, T? defaultValue = default)
        where T : notnull
    {
        var serializer = _provider.GetService<IViewStateSerializer<T>>();

        if (serializer != null)
        {
            serializer.Write(typeof(T), ref this, value, defaultValue);
        }
        else
        {
            Write(typeof(T), value, defaultValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        if (_span.Length < 1) Grow();

        _span[0] = value;
        Skip(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int length)
    {
        while (_span.Length < length)
        {
            Grow();
        }

        return _span.Slice(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(ViewStateWriterReservation reservation)
    {
        return _owner.Memory.Span.Slice(reservation.Offset, reservation.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int length)
    {
        _span = _span.Slice(length);
        _length += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AllocateUnsafe(int length)
    {
        var span = _span.Slice(0, length);
        Skip(length);
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ViewStateWriterReservation Reserve(int length)
    {
        var span = new ViewStateWriterReservation(_length, length);
        Skip(length);
        return span;
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

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _owner.Dispose();
        }

        _isDisposed = true;
    }
}

public readonly struct ViewStateWriterReservation
{
    public readonly int Offset;
    public readonly int Length;

    public ViewStateWriterReservation(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }
}