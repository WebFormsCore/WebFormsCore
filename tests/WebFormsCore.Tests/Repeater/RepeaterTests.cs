namespace WebFormsCore.Tests;

public class RepeaterTests
{
    [Fact]
    public async Task TypedRepeater()
    {
        await using var result = await RenderAsync("Repeater/Pages/TypedRepeater.aspx");

        await Verify(result.Html);
    }

    [Fact]
    public async Task Repeater()
    {
        await using var result = await RenderAsync("Repeater/Pages/Repeater.aspx");

        await Verify(result.Html);
    }
}
