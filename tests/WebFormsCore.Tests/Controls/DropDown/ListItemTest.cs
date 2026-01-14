using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.DropDown;

public class ListItemTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new DropDownList 
        { 
            ID = "ddl",
            Items =
            [
                new ListItem("Item 1", "1")
                {
                    Enabled = false,
                    Attributes = 
                    {
                        ["data-custom"] = "hello"
                    }
                },
                new ListItem("Item 2", "2")
                {
                    Selected = true
                }
            ]
        });

        var option1 = result.Browser.QuerySelector("#ddl option[value='1']");
        var option2 = result.Browser.QuerySelector("#ddl option[value='2']");

        Assert.NotNull(option1);
        Assert.NotNull(option2);

        Assert.Equal("hello", await option1!.GetAttributeAsync("data-custom"));
        Assert.Equal("true", await option1!.GetAttributeAsync("disabled"));
        
        Assert.Equal("true", await option2!.GetAttributeAsync("selected"));
    }

    [Fact]
    public void ListItemTextFallback()
    {
        // Unit test part
        var item = new ListItem { Value = "OnlyValue" };
        Assert.Equal("OnlyValue", item.Text);
        
        item.Text = "Overridden";
        Assert.Equal("Overridden", item.Text);
        Assert.Equal("OnlyValue", item.Value);
    }

    [Fact]
    public void ListItemValueFallback()
    {
        var item = new ListItem { Text = "OnlyText" };
        Assert.Equal("OnlyText", item.Value);
        Assert.Equal("OnlyText", item.Text);
        
        item.Value = "OverriddenValue";
        Assert.Equal("OnlyText", item.Text);
        Assert.Equal("OverriddenValue", item.Value);
    }

    [Fact]
    public void ListItemConstructors()
    {
        var item1 = new ListItem();
        Assert.Equal(string.Empty, item1.Text);
        Assert.Equal(string.Empty, item1.Value);
        Assert.True(item1.Enabled);

        var item2 = new ListItem("Text");
        Assert.Equal("Text", item2.Text);
        Assert.Equal("Text", item2.Value);

        var item3 = new ListItem("Text", "Value");
        Assert.Equal("Text", item3.Text);
        Assert.Equal("Value", item3.Value);
        Assert.True(item3.Enabled);

        var item4 = new ListItem("Text", "Value", false);
        Assert.Equal("Text", item4.Text);
        Assert.Equal("Value", item4.Value);
        Assert.False(item4.Enabled);
    }

    [Fact]
    public void ListItemEquals()
    {
        var item1 = new ListItem("Text", "Value");
        var item2 = new ListItem("Text", "Value");
        var item3 = new ListItem("Different", "Value");
        var item4 = new ListItem("Text", "DifferentValue");

        Assert.True(item1.Equals(item2));
        Assert.False(item1.Equals(item3));
        Assert.False(item1.Equals(item4));
        Assert.False(item1.Equals(null));
    }

    [Fact]
    public void ListItemFromString()
    {
        var item = ListItem.FromString("Test");
        Assert.Equal("Test", item.Text);
        Assert.Equal("Test", item.Value);
    }

    [Fact]
    public void ListItemToString()
    {
        var item1 = new ListItem("Display", "Value");
        Assert.Equal("Display", item1.ToString());

        var item2 = new ListItem { Value = "ValueOnly" };
        Assert.Equal("ValueOnly", item2.ToString());
    }

    [Fact]
    public void ListItemAttributes()
    {
        var item = new ListItem("Item");
        Assert.NotNull(item.Attributes);
        
        item.Attributes.Add("data-id", "123");
        Assert.Equal("123", item.Attributes["data-id"]);

        var accessor = (IAttributeAccessor)item;
        accessor.SetAttribute("data-test", "value");
        Assert.Equal("value", accessor.GetAttribute("data-test"));
    }

    [Fact]
    public void ListItemEnabledDefault()
    {
        var item = new ListItem("Item");
        Assert.True(item.Enabled);
    }

    [Fact]
    public void ListItemSelectedDefault()
    {
        var item = new ListItem("Item");
        Assert.False(item.Selected);
    }

    [Fact]
    public void ListItemGetHashCode()
    {
        var item1 = new ListItem("Text", "Value");
        var item3 = new ListItem("Different", "Value");

        // GetHashCode should be consistent for the same object
        Assert.Equal(item1.GetHashCode(), item1.GetHashCode());
        
        // Different Text/Value should produce different hash codes
        Assert.NotEqual(item1.GetHashCode(), item3.GetHashCode());
    }

    [Fact]
    public void ListItemOperatorEquality()
    {
        var item1 = new ListItem("Text", "Value");
        var item2 = new ListItem("Text", "Value");
        var item3 = new ListItem("Different", "Value");

        Assert.True(item1 == item2);
        Assert.False(item1 == item3);
        Assert.True(item1 != item3);
        Assert.False(item1 != item2);
    }}