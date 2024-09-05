namespace WebFormsCore.Tests;

public class RepeaterTests
{
    [Fact]
    public async Task TypedRepeater()
    {
        await using var result = await RenderAsync<AssemblyControlTypeProvider>("Repeater/Pages/TypedRepeater.aspx");

        await Verify(await result.GetHtmlAsync());
    }

    [Fact]
    public async Task Repeater()
    {
        await using var result = await RenderAsync<AssemblyControlTypeProvider>("Repeater/Pages/Repeater.aspx");

        await Verify(await result.GetHtmlAsync());
    }
}
