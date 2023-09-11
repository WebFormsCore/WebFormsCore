using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.Internal;
using WebFormsCore.Providers;
using WebFormsCore.Serializer;
using WebFormsCore.UI;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebForms(this IServiceCollection services, Action<IWebFormsCoreBuilder>? configure = null)
    {
        var builder = services.AddWebFormsCore();
        configure?.Invoke(builder);
        return services;
    }

    public static IWebFormsCoreBuilder AddWebFormsCore(this IServiceCollection services)
    {
        var builder = new WebFormsCoreBuilder(services);

        services.TryAddSingleton<IViewStateManager, ViewStateManager>();

        services.TryAddSingleton<IWebFormsEnvironment, DefaultWebFormsEnvironment>();

        services.TryAddSingleton<IQueryableCountProvider, DefaultQueryableCountProvider>();

        services.AddScoped<ScopedControlContainer>();
        services.TryAddScoped(typeof(IControlFactory<>), typeof(ControlFactory<>));
        services.TryAddSingleton<IPageManager, PageManager>();
        services.TryAddSingleton<IWebFormsApplication, WebFormsApplications>();
        services.TryAddScoped<IWebObjectActivator, WebObjectActivator>();
        services.TryAddSingleton<IControlManager, DefaultControlManager>();

        builder.AddPooledControl<LiteralControl>();
        builder.AddPooledControl<LiteralHtmlControl>();
        builder.AddPooledControl<Literal>();
        builder.AddPooledControl<HtmlBody>();
        builder.AddPooledControl<HtmlLink>();
        builder.AddPooledControl<HtmlForm>();
        builder.AddPooledControl<HtmlStyle>();
        builder.AddPooledControl<HtmlImage>();

        services.TryAddSingleton<IDefaultViewStateSerializer, DefaultViewStateSerializer>();
        builder.AddViewStateSerializer<ArrayViewStateSerializer>();
        builder.AddViewStateSerializer<ListViewStateSerializer>();
        builder.AddViewStateSerializer<ViewStateObjectSerializer>();
        builder.AddViewStateSerializer<EnumViewStateSerializer>();
        builder.AddViewStateSerializer<NullableViewStateSerializer>();
        builder.AddViewStateSerializer<Type, TypeViewStateSerializer>();
        builder.AddViewStateSerializer<string, StringViewStateSerializer>();
        builder.AddViewStateSpanSerializer<char, StringViewStateSerializer>();
        builder.AddMarshalViewStateSerializer<int>();
        builder.AddMarshalViewStateSerializer<uint>();
        builder.AddMarshalViewStateSerializer<short>();
        builder.AddMarshalViewStateSerializer<ushort>();
        builder.AddMarshalViewStateSerializer<byte>();
        builder.AddMarshalViewStateSerializer<sbyte>();
        builder.AddMarshalViewStateSerializer<long>();
        builder.AddMarshalViewStateSerializer<ulong>();
        builder.AddMarshalViewStateSerializer<float>();
        builder.AddMarshalViewStateSerializer<double>();
        builder.AddMarshalViewStateSerializer<decimal>();
        builder.AddMarshalViewStateSerializer<bool>();
        builder.AddMarshalViewStateSerializer<char>();

        services.TryAddSingleton<IAttributeParser<string>, StringAttributeParser>();
        services.TryAddSingleton<IAttributeParser<int>, Int32AttributeParser>();
        services.TryAddSingleton<IAttributeParser<int?>, NullableAttributeParser<int>>();
        services.TryAddSingleton<IAttributeParser<bool>, BoolAttributeParser>();
        services.TryAddSingleton<IAttributeParser<Unit>, UnitAttributeParser>();
        services.TryAddSingleton<IAttributeParser<string[]>, ArrayAttributeParser<string>>();

        return builder;
    }

    public static IWebFormsCoreBuilder AddEnumAttributeParser<T>(this IWebFormsCoreBuilder builder)
        where T : struct, Enum
    {
        builder.Services.TryAddSingleton<IAttributeParser<T>, EnumAttributeParser<T>>();
        return builder;
    }

    public static IWebFormsCoreBuilder AddPooledControl<T>(this IWebFormsCoreBuilder builder, int maxAmount = 1024)
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

        builder.Services.TryAddSingleton<ObjectPool<T>>(
            new DefaultObjectPool<T>(new ControlObjectPolicy<T>(), maxAmount)
        );

        builder.Services.TryAddScoped<IControlFactory<T>, PooledControlFactory<T>>();

        return builder;
    }

    public static IWebFormsCoreBuilder AddMarshalViewStateSerializer<T>(this IWebFormsCoreBuilder builder)
        where T : struct
    {
        AddViewStateSerializer<T, MarshalViewStateSerializer<T>>(builder);
        return builder;
    }

    public static IWebFormsCoreBuilder AddViewStateSpanSerializer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSpanSerializer<T>
        where T : notnull
    {
        builder.Services.TryAddSingleton<TSerializer>();
        builder.Services.AddSingleton<IViewStateSpanSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return builder;
    }

    public static IWebFormsCoreBuilder AddViewStateSerializer<T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSerializer<T>
        where T : notnull
    {
        AddViewStateSerializer<TSerializer>(builder);
        builder.Services.AddSingleton<IViewStateSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return builder;
    }

    public static IWebFormsCoreBuilder AddViewStateSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSerializer
    {
        var services = builder.Services;
        var id = checked((byte)(DefaultViewStateSerializer.Offset + services.Count(i => i.ServiceType == typeof(ViewStateSerializerRegistration))));

        services.TryAddSingleton<TSerializer>();
        services.AddSingleton<IViewStateSerializer>(p => p.GetRequiredService<TSerializer>());
        services.AddSingleton(new ViewStateSerializerRegistration(id, typeof(TSerializer)));

        return builder;
    }
}
