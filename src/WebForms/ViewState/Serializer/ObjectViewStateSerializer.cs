using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace System.Web.Serializer;

public class ObjectViewStateSerializer : ViewStateSerializer<object?>
{
    private readonly IServiceProvider _provider;

    public ObjectViewStateSerializer(IServiceProvider provider)
    {
        _provider = provider;
    }

    public override bool TryWrite(object? value, Span<byte> span, out int length)
    {
        if (value is null)
        {
            span[0] = 0;
            length = 1;
            return true;
        }

        var type = value.GetType();
        var registration = _provider
            .GetServices<ViewStateSerializerRegistration>()
            .First(i => i.Type == type);

        span[0] = registration.Id;
        span = span.Slice(1);

        var serializer = (IViewStateSerializer)_provider.GetRequiredService(registration.SerializerType);

        if (!serializer.TryWrite(value, span, out length))
        {
            return false;
        }

        length += 1;
        return true;
    }

    public override object? Read(ReadOnlySpan<byte> span, out int length)
    {
        var id = span[0];

        if (id == 0)
        {
            length = 1;
            return null;
        }

        var registration = _provider
            .GetServices<ViewStateSerializerRegistration>()
            .First(i => i.Id == id);

        span = span.Slice(1);

        var serializer = (IViewStateSerializer)_provider.GetRequiredService(registration.SerializerType);
        var value = serializer.ReadObject(span, out length);

        length += 1;
        return value;
    }
}