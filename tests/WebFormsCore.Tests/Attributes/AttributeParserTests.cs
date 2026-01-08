using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.Attributes;
using Xunit;

namespace WebFormsCore.Tests.Attributes;

public enum TestEnum
{
    Value1,
    Value2
}

public class AttributeParserTests
{
    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void TestEnumParser()
    {
        var provider = CreateServiceProvider();

        var parser = provider.GetRequiredService<IAttributeParser<TestEnum>>();
        Assert.Equal(TestEnum.Value1, parser.Parse("Value1"));
        Assert.Equal(TestEnum.Value2, parser.Parse("Value2"));
    }

    [Fact]
    public void TestArrayParser()
    {
        var provider = CreateServiceProvider();

        var parser = provider.GetRequiredService<IAttributeParser<int[]>>();
        var result = parser.Parse("1,2,3");
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void TestListParser()
    {
        var provider = CreateServiceProvider();

        var parser = provider.GetRequiredService<IAttributeParser<List<int>>>();
        var result = parser.Parse("1,2,3");
        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Fact]
    public void TestIListParser()
    {
        var provider = CreateServiceProvider();

        var parser = provider.GetRequiredService<IAttributeParser<IList<int>>>();
        var result = parser.Parse("1,2,3");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0]);
    }

    [Fact]
    public void TestIReadOnlyListParser()
    {
        var provider = CreateServiceProvider();

        var parser = provider.GetRequiredService<IAttributeParser<IReadOnlyList<int>>>();
        var result = parser.Parse("1,2,3");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0]);
    }

    [Fact]
    public void TestUnsupportedType()
    {
        var provider = CreateServiceProvider();

        var exception = Assert.Throws<NotSupportedException>(() => provider.GetRequiredService<IAttributeParser<AttributeParserTests>>());
        Assert.Contains("is not supported by GenericAttributeParser", exception.Message);
    }

    [Fact]
    public void TestCaching()
    {
        var provider = CreateServiceProvider();

        var parser1 = provider.GetRequiredService<IAttributeParser<TestEnum>>();
        var parser2 = provider.GetRequiredService<IAttributeParser<TestEnum>>();

        Assert.Same(parser1, parser2);
    }
}

