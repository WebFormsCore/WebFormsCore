﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.Serializer;

public interface IViewStateSerializer
{
    bool CanSerialize(Type type);

    void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue);

    object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, object? defaultValue);

    bool StoreInViewState(Type type, object? value, object? defaultValue);

    void TrackViewState(Type type, object? value, ViewStateProvider provider);
}

public interface IDefaultViewStateSerializer : IViewStateSerializer
{
}

public interface IViewStateSerializer<T> : IViewStateSerializer
{
    void Write(Type type, ref ViewStateWriter writer, T? value, T? defaultValue);

    T? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, T? defaultValue);

    bool StoreInViewState(Type type, T? value, T? defaultValue);

    void TrackViewState(Type type, T? value, ViewStateProvider provider);
}

public interface IViewStateSpanSerializer<T>
{
    void Write(ref ViewStateWriter writer, scoped ReadOnlySpan<T> value);

    int Read(ref ViewStateReader reader, scoped Span<T> value);
}

public abstract class ViewStateSerializer<T> : IViewStateSerializer<T>
{
    public abstract void Write(Type type, ref ViewStateWriter writer, T? value, T? defaultValue);

    public abstract T? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, T? defaultValue);

    public abstract bool StoreInViewState(Type type, T? value, T? defaultValue);

    public abstract void TrackViewState(Type type, T? value, ViewStateProvider provider);

    public virtual bool CanSerialize(Type type)
    {
        return typeof(T) == type;
    }

    public virtual void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue)
    {
        Write(type, ref writer, (T?)value, defaultValue is null ? default : (T?)defaultValue);
    }

    public virtual object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, object? defaultValue)
    {
        return Read(type, ref reader, defaultValue is null ? default : (T?)defaultValue);
    }

    bool IViewStateSerializer.StoreInViewState(Type type, object? value, object? defaultValue)
    {
        if (value is not T t || defaultValue is not T d)
        {
            throw new InvalidOperationException("Invalid type");
        }

        return StoreInViewState(type, t, d);
    }

    void IViewStateSerializer.TrackViewState(Type type, object? value, ViewStateProvider provider)
    {
        if (value is null)
        {
            return;
        }

        if (value is not T t)
        {
            throw new InvalidOperationException("Invalid type");
        }

        TrackViewState(type, t, provider);
    }

}
