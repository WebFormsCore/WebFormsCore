using NSubstitute;
using WebFormsCore.Serializer;

namespace WebFormsCore.Tests;

public class ViewStateSerializer
{
    [Theory]
    [InlineData(new[] { "1", "2", "3" }, 2 + ((2 + 1) * 3))]
    [InlineData(new string[0], 2)]
    [InlineData(null, 2)]
    public void Array(string[]? expected, int expectedLength)
    {
        var provider = Substitute.For<IServiceProvider>();
        var stringViewSerializer = new StringViewStateSerializer();
        var arraySerializer = new ArrayViewStateSerializer();

        provider
            .GetService(typeof(IEnumerable<IViewStateSerializer>))
            .Returns(new IViewStateSerializer[] { stringViewSerializer, arraySerializer });

        // Write the value
        var writer = new ViewStateWriter(provider);

        writer.Write(expected);
        Assert.Equal(expectedLength, writer.Length);

        // Read the value
        using var owner = new ViewStateReaderOwner(writer.Memory, provider);
        using var reader = owner.CreateReader();

        var result = reader.Read<string[]>();
        Assert.Equal(expected, result);
        Assert.Equal(expectedLength, reader.Offset);

        writer.Dispose();
    }

    public static IEnumerable<object?[]> ListData()
    {
        yield return new object?[] { new List<string> { "1", "2", "3" }, 2 + ((2 + 1) * 3) };
        yield return new object?[] { new List<string>(), 2 };
        yield return new object?[] { null, 2 };
    }

    [Theory]
    [MemberData(nameof(ListData))]
    public void List(List<string>? expected, int expectedLength)
    {
        var provider = Substitute.For<IServiceProvider>();
        var stringViewSerializer = new StringViewStateSerializer();
        var arraySerializer = new ListViewStateSerializer();

        provider
            .GetService(typeof(IEnumerable<IViewStateSerializer>))
            .Returns(new IViewStateSerializer[] { stringViewSerializer, arraySerializer });

        // Write the value
        var writer = new ViewStateWriter(provider);

        writer.Write(expected);
        Assert.Equal(expectedLength, writer.Length);

        // Read the value
        using var owner = new ViewStateReaderOwner(writer.Memory, provider);
        using var reader = owner.CreateReader();

        var result = reader.Read<List<string>>();
        Assert.Equal(expected, result);
        Assert.Equal(expectedLength, reader.Offset);

        writer.Dispose();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void ListTest(int size)
    {
        var expected = new List<string>();

        for (var i = 0; i < size; i++)
        {
            expected.Add("Item");
        }

        var provider = Substitute.For<IServiceProvider>();
        var stringViewSerializer = new StringViewStateSerializer();
        var arraySerializer = new ListViewStateSerializer();

        provider
            .GetService(typeof(IEnumerable<IViewStateSerializer>))
            .Returns(new IViewStateSerializer[] { stringViewSerializer, arraySerializer });

        // Write the value
        var writer = new ViewStateWriter(provider);

        writer.Write(expected);

        // Read the value
        using var owner = new ViewStateReaderOwner(writer.Memory, provider);
        using var reader = owner.CreateReader();

        var result = reader.Read<List<string>>();
        Assert.Equal(expected, result);

        writer.Dispose();
    }
}
