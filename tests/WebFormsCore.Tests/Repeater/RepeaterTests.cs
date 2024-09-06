namespace WebFormsCore.Tests;

public class RepeaterTests
{
    [Fact]
    public async Task TypedRepeater()
    {
        await using var result = await RenderAsync<AssemblyControlTypeProvider>("Repeater/Pages/TypedRepeater.aspx");

        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Fact]
    public async Task Repeater()
    {
        await using var result = await RenderAsync<AssemblyControlTypeProvider>("Repeater/Pages/Repeater.aspx");

        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }
}
