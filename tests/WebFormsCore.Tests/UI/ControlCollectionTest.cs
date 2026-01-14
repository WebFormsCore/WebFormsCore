using WebFormsCore.UI;
using Xunit;
using System;

namespace WebFormsCore.Tests.UnitTests.UI;

public class ControlCollectionTest
{
    private class TestControl : Control { }

    [Fact]
    public void ReadOnly_ThrowsException()
    {
        var owner = new TestControl();
        var collection = new ControlCollection(owner);
        var child = new TestControl();

        // Use reflection to set read-only error message
        var method = typeof(ControlCollection).GetMethod("SetCollectionReadOnly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var errorMsg = "Collection is read-only for testing.";
        method?.Invoke(collection, new object[] { errorMsg });

        Assert.True(collection.IsReadOnly);
        var ex = Assert.Throws<InvalidOperationException>(() => collection.AddWithoutPageEvents(child));
        Assert.Equal(errorMsg, ex.Message);
    }

    [Fact]
    public void NamingContainerChildren_Tracked()
    {
        var page = new Page();
        var container = new TestNamingContainer { ID = "container" };
        var child = new TestControl { ID = "child" };
        
        page.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child);
        
        Assert.Single(container.Controls.NamingContainerChildren);
        Assert.Same(child, container.Controls.NamingContainerChildren[0]);
    }

    private class TestNamingContainer : Control, INamingContainer { }

    [Fact]
    public void Clear_RemovesAllControls()
    {
        var parent = new TestControl();
        var child1 = new TestControl();
        var child2 = new TestControl();
        
        parent.Controls.AddWithoutPageEvents(child1);
        parent.Controls.AddWithoutPageEvents(child2);
        
        Assert.Equal(2, parent.Controls.Count);
        
        parent.Controls.Clear();
        
        Assert.Empty(parent.Controls);
        Assert.Throws<InvalidOperationException>(() => child1.Parent);
        Assert.Throws<InvalidOperationException>(() => child2.Parent);
    }
}
