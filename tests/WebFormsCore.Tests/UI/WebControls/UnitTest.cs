using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.UnitTests.WebControls;

public class UnitTest
{
    [Fact]
    public void ParsePixel()
    {
        var unit = new Unit("10px");
        Assert.Equal(10, unit.Value);
        Assert.Equal(UnitType.Pixel, unit.Type);
        Assert.Equal("10px", unit.ToString());
    }

    [Fact]
    public void ParsePoint()
    {
        var unit = new Unit("12pt");
        Assert.Equal(12, unit.Value);
        Assert.Equal(UnitType.Point, unit.Type);
        Assert.Equal("12pt", unit.ToString());
    }

    [Fact]
    public void ParsePercentage()
    {
        var unit = new Unit("50%");
        Assert.Equal(50, unit.Value);
        Assert.Equal(UnitType.Percentage, unit.Type);
        Assert.Equal("50%", unit.ToString());
    }

    [Fact]
    public void ParseEmpty()
    {
        var unit = new Unit("");
        Assert.True(unit.IsEmpty);
    }

    [Fact]
    public void ParseVariousUnits()
    {
        Assert.Equal(UnitType.Pica, new Unit("1pc").Type);
        Assert.Equal(UnitType.Inch, new Unit("1in").Type);
        Assert.Equal(UnitType.Mm, new Unit("1mm").Type);
        Assert.Equal(UnitType.Cm, new Unit("1cm").Type);
        Assert.Equal(UnitType.Em, new Unit("1em").Type);
        Assert.Equal(UnitType.Ex, new Unit("1ex").Type);
    }
}
