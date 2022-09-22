﻿using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.Internal;
using WebFormsCore.Serializer;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebFormsCore(this IServiceCollection services)
    {
        services.AddSingleton<PageFactory>();
        services.AddSingleton<IWebFormsApplication, WebFormsApplications>();
        services.AddScoped<IWebObjectActivator, WebObjectActivator>();

        services.AddSingleton<ObjectPool<LiteralControl>>(
            new DefaultObjectPool<LiteralControl>(new ControlObjectPolicy<LiteralControl>())
        );

        services.AddSingleton<ObjectPool<HtmlGenericControl>>(
            new DefaultObjectPool<HtmlGenericControl>(new ControlObjectPolicy<HtmlGenericControl>())
        );

        services.AddSingleton<IViewStateSerializer<object?>, ObjectViewStateSerializer>();
        services.AddViewStateSerializer<string?, StringViewStateSerializer>();
        services.AddViewStateSerializer<int>();
        services.AddViewStateSerializer<uint>();
        services.AddViewStateSerializer<short>();
        services.AddViewStateSerializer<ushort>();
        services.AddViewStateSerializer<byte>();
        services.AddViewStateSerializer<sbyte>();
        services.AddViewStateSerializer<long>();
        services.AddViewStateSerializer<ulong>();
        services.AddViewStateSerializer<float>();
        services.AddViewStateSerializer<double>();
        services.AddViewStateSerializer<decimal>();
        services.AddViewStateSerializer<bool>();
        services.AddViewStateSerializer<char>();
        services.AddViewStateSerializer<DateTime>();
        services.AddViewStateSerializer<DateTimeOffset>();

        return services;
    }

    public static IServiceCollection AddViewStateSerializer<T>(this IServiceCollection services)
        where T : struct
    {
        AddViewStateSerializer<T, MarshalViewStateSerializer<T>>(services);
        return services;
    }

    public static IServiceCollection AddViewStateSerializer<T, TSerializer>(this IServiceCollection services)
        where TSerializer : class, IViewStateSerializer<T>
    {
        var offset = (byte)(1 + services.Count(i => i.ServiceType == typeof(ViewStateSerializerRegistration)));
        
        services.AddSingleton<IViewStateSerializer<T>, TSerializer>();
        services.AddSingleton(new ViewStateSerializerRegistration(offset, typeof(T), typeof(IViewStateSerializer<T>)));

        return services;
    }
}
