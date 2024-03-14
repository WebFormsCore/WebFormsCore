using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Serializer;

namespace WebFormsCore;

public static class ViewStateReaderExtensions
{
    public static void Read<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ViewStateReader reader)
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
        Compact = provider.GetService<IOptions<ViewStateOptions>>()?.Value.Compact ?? true;
    }

    public bool Compact { get; }

    public int Offset => _offset;

    public object? ReadObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, object? defaultValue = default)
    {
        var objSerializer = Provider
            .GetServices<IViewStateSerializer>()
            .FirstOrDefault(i => i.CanSerialize(type));

        objSerializer ??= Provider.GetRequiredService<IDefaultViewStateSerializer>();

        if (objSerializer == null)
        {
            throw new InvalidOperationException($"No serializer found for type {type}");
        }

        return objSerializer.Read(type, ref this, defaultValue);
    }

    public T? Read<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(T? defaultValue = default)
    {
        var serializer = Provider.GetService<IViewStateSerializer<T>>();

        if (serializer != null)
        {
            return serializer.Read(typeof(T), ref this, defaultValue);
        }

        return (T?) ReadObject(typeof(T), defaultValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var span = _span.Slice(0, length);
        _span = _span.Slice(length);
        _offset += length;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        var value = _span[0];
        _span = _span.Slice(1);
        _offset += 1;
        return value;
    }

    public void Dispose()
    {
        _owner.Offset = _offset;
    }
}

public sealed class ViewStateReaderOwner : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly Memory<byte> _memory;
    private readonly IDisposable? _disposable;

    public ViewStateReaderOwner(Memory<byte> memory, IServiceProvider provider, int offset = 0, ushort controlCount = 0, IDisposable? disposable = null)
    {
        Offset = offset;
        ControlCount = controlCount;
        _memory = memory;
        _provider = provider;
        _disposable = disposable;
    }

    public int Offset { get; set; }

    public ushort ControlCount { get; }

    public ViewStateReader CreateReader()
    {
        return new ViewStateReader(_memory.Span.Slice(Offset), _provider, this);
    }

    public void Dispose()
    {
        _disposable?.Dispose();
    }
}
