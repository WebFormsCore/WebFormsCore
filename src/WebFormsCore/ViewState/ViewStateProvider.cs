using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Serializer;

namespace WebFormsCore;

public readonly struct ViewStateProvider
{
    private readonly IServiceProvider _provider;

    public ViewStateProvider(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void TrackViewState(Type type, object? value)
    {
        var objSerializer = _provider
            .GetServices<IViewStateSerializer>()
            .FirstOrDefault(i => i.CanSerialize(type));

        objSerializer ??= _provider.GetRequiredService<IDefaultViewStateSerializer>();

        objSerializer.TrackViewState(type, value, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrackViewState<T>(T? value)
    {
        var serializer = _provider.GetService<IViewStateSerializer<T>>();

        if (serializer != null)
        {
            serializer.TrackViewState(typeof(T), value, this);
            return;
        }

        TrackViewState(typeof(T), value);
    }
}
