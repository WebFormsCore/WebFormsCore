using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.Internal;
using WebFormsCore.Options;
using WebFormsCore.Serializer;
using WebFormsCore.UI;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebForms(this IServiceCollection services)
    {
        services.TryAddSingleton<IViewStateManager, ViewStateManager>();

        services.TryAddSingleton<IWebFormsEnvironment, DefaultWebFormsEnvironment>();

        services.AddScoped<ScopedControlContainer>();
        services.TryAddScoped(typeof(IControlFactory<>), typeof(ControlFactory<>));
        services.TryAddSingleton<IPageManager, PageManager>();
        services.TryAddSingleton<IWebFormsApplication, WebFormsApplications>();
        services.TryAddScoped<IWebObjectActivator, WebObjectActivator>();
        services.TryAddSingleton<IControlManager, DefaultControlManager>();

        services.AddPooledControl<LiteralControl>();
        services.AddPooledControl<LiteralHtmlControl>();
        services.AddPooledControl<Literal>();
        services.AddPooledControl<HtmlBody>();
        services.AddPooledControl<HtmlLink>();
        services.AddPooledControl<HtmlForm>();

        services.AddSingleton<IDefaultViewStateSerializer, DefaultViewStateSerializer>();
        services.AddViewStateSerializer<ArrayViewStateSerializer>();
        services.AddViewStateSerializer<ListViewStateSerializer>();
        services.AddViewStateSerializer<ViewStateObjectSerializer>();
        services.AddViewStateSerializer<EnumViewStateSerializer>();
        services.AddViewStateSerializer<NullableViewStateSerializer>();
        services.AddViewStateSerializer<Type, TypeViewStateSerializer>();
        services.AddViewStateSerializer<string, StringViewStateSerializer>();
        services.AddViewStateSpanSerializer<char, StringViewStateSerializer>();
        services.AddMarshalViewStateSerializer<int>();
        services.AddMarshalViewStateSerializer<uint>();
        services.AddMarshalViewStateSerializer<short>();
        services.AddMarshalViewStateSerializer<ushort>();
        services.AddMarshalViewStateSerializer<byte>();
        services.AddMarshalViewStateSerializer<sbyte>();
        services.AddMarshalViewStateSerializer<long>();
        services.AddMarshalViewStateSerializer<ulong>();
        services.AddMarshalViewStateSerializer<float>();
        services.AddMarshalViewStateSerializer<double>();
        services.AddMarshalViewStateSerializer<decimal>();
        services.AddMarshalViewStateSerializer<bool>();
        services.AddMarshalViewStateSerializer<char>();

        services.TryAddSingleton<IAttributeParser<string>, StringAttributeParser>();
        services.TryAddSingleton<IAttributeParser<int>, Int32AttributeParser>();
        services.TryAddSingleton<IAttributeParser<int?>, NullableAttributeParser<int>>();
        services.TryAddSingleton<IAttributeParser<bool>, BoolAttributeParser>();
        services.TryAddSingleton<IAttributeParser<Unit>, UnitAttributeParser>();
        services.TryAddSingleton<IAttributeParser<string[]>, ArrayAttributeParser<string>>();

        return services;
    }

    public static IServiceCollection AddPooledControl<T>(this IServiceCollection services, int maxAmount = 1024)
        where T : Control, new()
    {
        if (typeof(T).GetCustomAttributes<ViewPathAttribute>().Any())
        {
            throw new InvalidOperationException("Cannot pool a control with a view path");
        }

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException("Cannot pool a control that implements IDisposable");
        }

        if (typeof(IAsyncDisposable).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException("Cannot pool a control that implements IAsyncDisposable");
        }

        services.AddSingleton<ObjectPool<T>>(
            new DefaultObjectPool<T>(new ControlObjectPolicy<T>(), maxAmount)
        );

        services.AddScoped<IControlFactory<T>, PooledControlFactory<T>>();

        return services;
    }

    public static IServiceCollection AddMarshalViewStateSerializer<T>(this IServiceCollection services)
        where T : struct
    {
        AddViewStateSerializer<T, MarshalViewStateSerializer<T>>(services);
        return services;
    }

    public static IServiceCollection AddViewStateSpanSerializer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>(this IServiceCollection services)
        where TSerializer : class, IViewStateSpanSerializer<T>
        where T : notnull
    {
        services.TryAddSingleton<TSerializer>();
        services.AddSingleton<IViewStateSpanSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return services;
    }

    public static IServiceCollection AddViewStateSerializer<T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IServiceCollection services)
        where TSerializer : class, IViewStateSerializer<T>
        where T : notnull
    {
        AddViewStateSerializer<TSerializer>(services);
        services.AddSingleton<IViewStateSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return services;
    }

    public static IServiceCollection AddViewStateSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IServiceCollection services)
        where TSerializer : class, IViewStateSerializer
    {
        var id = checked((byte)(DefaultViewStateSerializer.Offset + services.Count(i => i.ServiceType == typeof(ViewStateSerializerRegistration))));

        services.TryAddSingleton<TSerializer>();
        services.AddSingleton<IViewStateSerializer>(p => p.GetRequiredService<TSerializer>());
        services.AddSingleton(new ViewStateSerializerRegistration(id, typeof(TSerializer)));

        return services;
    }
}
