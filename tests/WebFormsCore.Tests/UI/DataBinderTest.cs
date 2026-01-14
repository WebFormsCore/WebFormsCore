using WebFormsCore.UI;

namespace WebFormsCore.Tests.UnitTests.UI;

public class DataBinderTest
{
    [Fact]
    public void EvalTest()
    {
        var item = new { Name = "John", Age = 30 };
        Assert.Equal("John", DataBinder.Eval(item, "Name"));
        Assert.Equal(30, DataBinder.Eval(item, "Age"));
    }

    [Fact]
    public void Eval_NestedProperty_ReturnsValue()
    {
        var item = new { User = new { FirstName = "John" } };
        Assert.Equal("John", DataBinder.Eval(item, "User.FirstName"));
    }

    [Fact]
    public void Eval_NestedProperty_Null_ReturnsNull()
    {
        var item = new { User = (object?)null };
        Assert.Null(DataBinder.Eval(item, "User.FirstName"));
    }

    [Fact]
    public void Eval_PropertyNotFound_Throws()
    {
        var item = new { Name = "John" };
        Assert.Throws<InvalidOperationException>(() => DataBinder.Eval(item, "NotFound"));
    }

    private class MyContainer : IDataItemContainer
    {
        public object? DataItem { get; set; }
        public int DataItemIndex => 0;
        public int DisplayIndex => 0;
    }

    [Fact]
    public void GetDataItem_FromContainer()
    {
        var item = new object();
        var container = new MyContainer { DataItem = item };
        Assert.Same(item, DataBinder.GetDataItem(container));
    }

    [Fact]
    public void GetDataItem_Hierarchy()
    {
        var container = new MyHierarchyContainer { DataItem = "Target" };
        var child = new MyHierarchyControl();
        container.Controls.AddWithoutPageEvents(child);
        
        var result = DataBinder.GetDataItem(child);
        Assert.Equal("Target", result);
    }

    private class MyHierarchyControl : Control { }
    private class MyHierarchyContainer : Control, IDataItemContainer {
        public object? DataItem { get; set; }
        public int DataItemIndex => 0;
        public int DisplayIndex => 0;
    }
}
