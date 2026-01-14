using WebFormsCore.UI;
using Xunit;
using System.Linq;

namespace WebFormsCore.Tests.UnitTests.UI;

public class ControlTest
{
    private class TestControl : Control
    {
    }

    private class TestNamingContainer : Control, INamingContainer
    {
    }

    [Fact]
    public void Controls_AddedHierarchy_SetsParent()
    {
        var parent = new TestControl();
        var child = new TestControl();

        parent.Controls.AddWithoutPageEvents(child);

        Assert.Same(parent, child.Parent);
        Assert.Contains(child, parent.Controls);
    }

    [Fact]
    public void UniqueID_ReflectsNamingContainer()
    {
        var page = new Page();
        var root = new TestNamingContainer { ID = "root" };
        var container = new TestNamingContainer { ID = "container" };
        var child = new TestControl { ID = "child" };

        page.Controls.AddWithoutPageEvents(root);
        root.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child);

        Assert.Equal("root$container$child", child.UniqueID);
    }

    [Fact]
    public void Swap_ChangesOrder()
    {
        var parent = new TestControl();
        var child1 = new TestControl { ID = "1" };
        var child2 = new TestControl { ID = "2" };

        parent.Controls.AddWithoutPageEvents(child1);
        parent.Controls.AddWithoutPageEvents(child2);

        Assert.Equal(0, parent.Controls.ToList().IndexOf(child1));
        Assert.Equal(1, parent.Controls.ToList().IndexOf(child2));

        parent.Controls.Swap(child1, 1);

        Assert.Equal(1, parent.Controls.ToList().IndexOf(child1));
        Assert.Equal(0, parent.Controls.ToList().IndexOf(child2));
    }

    [Fact]
    public void MoveToLast_ChangesOrder()
    {
        var parent = new TestControl();
        var child1 = new TestControl { ID = "1" };
        var child2 = new TestControl { ID = "2" };
        var child3 = new TestControl { ID = "3" };

        parent.Controls.AddWithoutPageEvents(child1);
        parent.Controls.AddWithoutPageEvents(child2);
        parent.Controls.AddWithoutPageEvents(child3);

        parent.Controls.MoveToLast(child1);

        var list = parent.Controls.ToList();
        Assert.Equal(child2, list[0]);
        Assert.Equal(child3, list[1]);
        Assert.Equal(child1, list[2]);
    }

    [Fact]
    public void MoveToFront_ChangesOrder()
    {
        var parent = new TestControl();
        var child1 = new TestControl { ID = "1" };
        var child2 = new TestControl { ID = "2" };
        var child3 = new TestControl { ID = "3" };

        parent.Controls.AddWithoutPageEvents(child1);
        parent.Controls.AddWithoutPageEvents(child2);
        parent.Controls.AddWithoutPageEvents(child3);

        parent.Controls.MoveToFront(child3);

        var list = parent.Controls.ToList();
        Assert.Equal(child3, list[0]);
        Assert.Equal(child1, list[1]);
        Assert.Equal(child2, list[2]);
    }

    [Fact]
    public void Clear_RemovesAllControls()
    {
        var parent = new TestControl();
        parent.Controls.AddWithoutPageEvents(new TestControl());
        parent.Controls.AddWithoutPageEvents(new TestControl());

        Assert.Equal(2, parent.Controls.Count);

        parent.Controls.Clear();

        Assert.Empty(parent.Controls);
    }

    [Fact]
    public void AutomaticID_IsGenerated()
    {
        var page = new Page();
        var container = new TestNamingContainer { ID = "container" };
        var child1 = new TestControl();
        var child2 = new TestControl();

        page.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child1);
        container.Controls.AddWithoutPageEvents(child2);

        // Accessing UniqueID should trigger ID generation if it's a NamingContainer
        Assert.NotNull(child1.UniqueID);
        Assert.NotNull(child2.UniqueID);

        Assert.NotNull(child1.ID);
        Assert.NotNull(child2.ID);
        Assert.NotEqual(child1.ID, child2.ID);
        Assert.StartsWith("c", child1.ID);
    }

    [Fact]
    public void ClientID_Inherit_ReplacesSeparator()
    {
        var page = new Page();
        var container = new TestNamingContainer { ID = "container" };
        var child = new TestControl { ID = "child" };
        page.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child);

        // UniqueID uses '$' by default as a separator if no override
        Assert.Equal("container$child", child.UniqueID);
        Assert.Equal("container_child", child.ClientID);
    }

    [Fact]
    public void Controls_Remove_UpdatesParent()
    {
        var parent = new TestControl();
        var child = new TestControl();
        parent.Controls.AddWithoutPageEvents(child);

        Assert.Equal(parent, child.Parent);
        Assert.True(parent.Controls.Contains(child));

        parent.Controls.Remove(child);

        Assert.Throws<InvalidOperationException>(() => child.Parent);
        Assert.False(parent.Controls.Contains(child));
        Assert.Empty(parent.Controls);
    }

    [Fact]
    public void Controls_Swap_UpdatesIndex()
    {
        var parent = new TestControl();
        var child1 = new TestControl { ID = "1" };
        var child2 = new TestControl { ID = "2" };
        var child3 = new TestControl { ID = "3" };
        parent.Controls.AddWithoutPageEvents(child1);
        parent.Controls.AddWithoutPageEvents(child2);
        parent.Controls.AddWithoutPageEvents(child3);

        parent.Controls.Swap(child1, 2);

        Assert.Equal(0, parent.Controls.IndexOf(child2));
        Assert.Equal(1, parent.Controls.IndexOf(child3));
        Assert.Equal(2, parent.Controls.IndexOf(child1));
    }

    [Fact]
    public void ClientID_Static_UsesID()
    {
        var page = new Page();
        var container = new TestNamingContainer { ID = "container" };
        var child = new TestControl { ID = "child", ClientIDMode = ClientIDMode.Static };
        page.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child);

        Assert.Equal("container$child", child.UniqueID);
        Assert.Equal("child", child.ClientID);
    }

    [Fact]
    public void ClientID_Predictable_ConcatenatesWithUnderScore()
    {
        var page = new Page();
        var container = new TestNamingContainer { ID = "container" };
        var child = new TestControl { ID = "child", ClientIDMode = ClientIDMode.Predictable };
        page.Controls.AddWithoutPageEvents(container);
        container.Controls.AddWithoutPageEvents(child);

        Assert.Equal("container_child", child.ClientID);
    }

    [Fact]
    public void Visible_InheritsFromParent()
    {
        var parent = new TestControl { Visible = false };
        var child = new TestControl { Visible = true };
        parent.Controls.AddWithoutPageEvents(child);

        Assert.False(child.Visible);
        Assert.True(child.SelfVisible);
    }

    [Fact]
    public void FindControl_FindsChild()
    {
        var page = new Page();
        var parent = new TestNamingContainer();
        page.Controls.AddWithoutPageEvents(parent);
        
        var child = new TestControl { ID = "child" };
        var grandchild = new TestNamingContainer { ID = "grandchild" };
        var target = new TestControl { ID = "target" };

        parent.Controls.AddWithoutPageEvents(child);
        child.Controls.AddWithoutPageEvents(grandchild);
        grandchild.Controls.AddWithoutPageEvents(target);

        // parent IS a naming container, so it can find its children and nested naming containers (but not controls inside those)
        Assert.Same(child, parent.FindControl("child"));
        Assert.Same(grandchild, parent.FindControl("grandchild"));
        Assert.Null(parent.FindControl("target")); // target is inside grandchild naming container

        // grandchild IS a naming container
        Assert.Same(target, grandchild.FindControl("target"));

        // UniqueID search
        Assert.Same(target, page.FindControl("c0$grandchild$target"));
    }

    [Fact]
    public void AutomaticID_IsGeneratedCorrectLength()
    {
        var parent = new TestNamingContainer();
        var children = new Control[130];
        for (int i = 0; i < 130; i++)
        {
            children[i] = new Control();
            parent.Controls.AddWithoutPageEvents(children[i]);
        }
        
        Assert.Equal("c127", children[127].ID);
        Assert.Equal("c128", children[128].ID);
        Assert.Equal("c129", children[129].ID);
    }

    [Fact]
    public void HasControls_ReflectsChildCount()
    {
        var parent = new TestControl();
        Assert.False(parent.HasControls());

        var child = new TestControl();
        parent.Controls.AddWithoutPageEvents(child);
        Assert.True(parent.HasControls());

        parent.Controls.Clear();
        Assert.False(parent.HasControls());
    }

    [Fact]
    public async Task FindControl_WithCollectionInitializer_FindsChildren()
    {
        // This test replicates the pattern used in Selenium tests:
        // fixture.StartAsync(type, () => new Panel { Controls = [...] })
        var page = new Page();
        
        // Create control with children via collection initializer
        var container = new TestControl
        {
            Controls =
            [
                new TestControl { ID = "child1" },
                new TestControl { ID = "child2" }
            ]
        };
        
        // Add container to page (like SeleniumFixture.StartAsync does)
        await page.Controls.AddAsync(container);
        
        // FindControl should find the children
        var found1 = page.FindControl("child1");
        var found2 = page.FindControl("child2");
        
        Assert.NotNull(found1);
        Assert.NotNull(found2);
        Assert.Equal("child1", found1.ID);
        Assert.Equal("child2", found2.ID);
    }
}
