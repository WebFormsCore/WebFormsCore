using WebFormsCore.TestFramework;
using WebFormsCore.Tests.Controls.Buttons.Pages;

namespace WebFormsCore.Tests.Controls.Buttons;

public class ButtonTest
{
    [Theory, ClassData(typeof(TestTypeData))]
    public async Task Click(TestType type)
    {
        await using var result = await StartAsync<ClickTest>(type);

        Assert.Empty(result.Control.lblResult.Text);

        await result.GetRequiredElement(result.Control.btnSetResult).ClickAsync();

        Assert.Equal("Success", result.GetRequiredElement(result.Control.lblResult).Text);
    }
}
