using System;
using System.Collections.Generic;
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

        services.TryAddScoped<ScopedControlContainer>();
        services.TryAddScoped(typeof(IControlFactory<>), typeof(ControlFactory<>));
        services.TryAddSingleton<IPageManager, PageManager>();
        services.TryAddSingleton<IWebFormsApplication, WebFormsApplications>();
        services.TryAddScoped<IWebObjectActivator, WebObjectActivator>();
        services.TryAddSingleton<IControlManager, DefaultControlManager>();

        builder.TryAddPooledControl<LiteralControl>();
        builder.TryAddPooledControl<LiteralHtmlControl>();
        builder.TryAddPooledControl<Literal>();
        builder.TryAddPooledControl<HtmlBody>();
        builder.TryAddPooledControl<HtmlLink>();
        builder.TryAddPooledControl<HtmlLink>();
        builder.TryAddPooledControl<HtmlForm>();
        builder.TryAddPooledControl<HtmlStyle>();
        builder.TryAddPooledControl<HtmlImage>();
        builder.TryAddPooledControl<RequiredFieldValidator>();
        builder.TryAddPooledControl<Panel>();
        builder.TryAddPooledControl<PlaceHolder>();
        builder.TryAddPooledControl<LinkButton>();
        builder.TryAddPooledControl<Button>();

        services.TryAddSingleton<IDefaultViewStateSerializer, DefaultViewStateSerializer>();
        builder.TryAddViewStateSerializer<ArrayViewStateSerializer>();
        builder.TryAddViewStateSerializer<ListViewStateSerializer>();
        builder.TryAddViewStateSerializer<ViewStateObjectSerializer>();
        builder.TryAddViewStateSerializer<EnumViewStateSerializer>();
        builder.TryAddViewStateSerializer<NullableViewStateSerializer>();
        builder.TryAddViewStateSerializer<Type, TypeViewStateSerializer>();
        builder.TryAddViewStateSerializer<string, StringViewStateSerializer>();
        builder.TryAddViewStateSpanSerializer<char, StringViewStateSerializer>();
        builder.TryAddMarshalViewStateSerializer<int>();
        builder.TryAddMarshalViewStateSerializer<uint>();
        builder.TryAddMarshalViewStateSerializer<short>();
        builder.TryAddMarshalViewStateSerializer<ushort>();
        builder.TryAddMarshalViewStateSerializer<byte>();
        builder.TryAddMarshalViewStateSerializer<sbyte>();
        builder.TryAddMarshalViewStateSerializer<long>();
        builder.TryAddMarshalViewStateSerializer<ulong>();
        builder.TryAddMarshalViewStateSerializer<float>();
        builder.TryAddMarshalViewStateSerializer<double>();
        builder.TryAddMarshalViewStateSerializer<decimal>();
        builder.TryAddMarshalViewStateSerializer<bool>();
        builder.TryAddMarshalViewStateSerializer<char>();

        services.TryAddSingleton<IAttributeParser<string>, StringAttributeParser>();
        services.TryAddSingleton<IAttributeParser<Type>, TypeAttributeParser>();
        services.TryAddSingleton<IAttributeParser<int>, Int32AttributeParser>();
        services.TryAddSingleton<IAttributeParser<int?>, NullableAttributeParser<int>>();
        services.TryAddSingleton<IAttributeParser<bool>, BoolAttributeParser>();
        services.TryAddSingleton<IAttributeParser<Unit>, UnitAttributeParser>();
        services.TryAddSingleton<IAttributeParser<string[]>, ArrayAttributeParser<string>>();
        services.TryAddSingleton<IAttributeParser<IReadOnlyList<string>>, ArrayAttributeParser<string>>();
        services.TryAddSingleton<IAttributeParser<List<string>>, ListAttributeParser<string>>();
        services.TryAddSingleton<IAttributeParser<IList<string>>, ListAttributeParser<string>>();

        return builder;
    }

    public static IWebFormsCoreBuilder TryAddEnumAttributeParser<T>(this IWebFormsCoreBuilder builder)
        where T : struct, Enum
    {
        builder.Services.TryAddSingleton<IAttributeParser<T>, EnumAttributeParser<T>>();
        return builder;
    }

    public static IWebFormsCoreBuilder TryAddPooledControl<T>(this IWebFormsCoreBuilder builder, int maxAmount = 1024)
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

    public static IWebFormsCoreBuilder TryAddMarshalViewStateSerializer<T>(this IWebFormsCoreBuilder builder)
        where T : struct
    {
        TryAddViewStateSerializer<T, MarshalViewStateSerializer<T>>(builder);
        return builder;
    }

    public static IWebFormsCoreBuilder TryAddViewStateSpanSerializer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSpanSerializer<T>
        where T : notnull
    {
        builder.Services.TryAddSingleton<TSerializer>();
        builder.Services.TryAddSingleton<IViewStateSpanSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return builder;
    }

    public static IWebFormsCoreBuilder TryAddViewStateSerializer<T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSerializer<T>
        where T : notnull
    {
        TryAddViewStateSerializer<TSerializer>(builder);
        builder.Services.TryAddSingleton<IViewStateSerializer<T>>(p => p.GetRequiredService<TSerializer>());
        return builder;
    }

    public static IWebFormsCoreBuilder TryAddViewStateSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TSerializer>(this IWebFormsCoreBuilder builder)
        where TSerializer : class, IViewStateSerializer
    {
        var services = builder.Services;

        if (services.Any(IsViewStateSerializerRegistration))
        {
            return builder;
        }

        var id = checked((byte)(DefaultViewStateSerializer.Offset + services.Count(i => i.ServiceType == typeof(ViewStateSerializerRegistration))));

        services.TryAddSingleton<TSerializer>();
        services.AddSingleton<IViewStateSerializer>(p => p.GetRequiredService<TSerializer>());
        services.AddSingleton(new ViewStateSerializerRegistration(id, typeof(TSerializer)));

        return builder;

        static bool IsViewStateSerializerRegistration(ServiceDescriptor i)
        {
            return i.ServiceType == typeof(ViewStateSerializerRegistration) &&
                   i.ImplementationInstance is ViewStateSerializerRegistration registration &&
                   registration.SerializerType == typeof(TSerializer);
        }
    }
}
