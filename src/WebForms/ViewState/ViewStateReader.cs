using Microsoft.Extensions.DependencyInjection;
using System.Web.Serializer;

namespace System.Web;

public static class ViewStateReaderExtensions
{
    public static void Read<T>(ViewStateReader reader)
        where T : IViewStateObject
    {
        var value = ActivatorUtilities.CreateInstance<T>(reader.Provider);
        value.ReadViewState(ref reader);
    }
}

public ref struct ViewStateReader
{
    internal readonly IServiceProvider Provider;
    private Span<byte> _span;

    public ViewStateReader(Span<byte> span, IServiceProvider provider)
    {
        _span = span;
        Provider = provider;
    }

    public T Read<T>()
    {
        var serializer = Provider.GetRequiredService<IViewStateSerializer<T>>();
        var value = serializer.Read(_span, out var length);

        _span = _span.Slice(length);
        return value;
    }
}