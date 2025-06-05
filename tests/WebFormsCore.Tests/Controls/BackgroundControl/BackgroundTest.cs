using WebFormsCore.Tests.Controls.BackgroundControl.Pages;
using WebFormsCore.Tests.Controls.Buttons.Pages;

namespace WebFormsCore.Tests.Controls.BackgroundControl;

public class BackgroundTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task LoadControlInBackground(Browser type)
    {
        await using var result = await fixture.StartAsync<SlowPage>(type);

        Assert.Equal(SlowPage.SlowControlCount, result.Control.CallCount);
        Assert.InRange(result.Control.ElapsedMilliseconds, 0, SlowPage.MaxElapsedMilliseconds);

        await result.Control.btnPostback.PostBackAsync();

        Assert.Equal(SlowPage.SlowControlCount, result.Control.CallCount);
        Assert.InRange(result.Control.ElapsedMilliseconds, 0, SlowPage.MaxElapsedMilliseconds);
    }
}
