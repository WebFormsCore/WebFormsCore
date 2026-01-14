using WebFormsCore.UI;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.Tests.UnitTests.UI;

public class ControlRemovalTest
{
    private class TestControl : Control
    {
        public bool Unloaded { get; set; }
        public bool PublicGenerateAutomaticID => GenerateAutomaticID;
        protected override ValueTask OnUnloadAsync(CancellationToken token)
        {
            Unloaded = true;
            return base.OnUnloadAsync(token);
        }
    }

    private class TestNamingContainer : Control, INamingContainer
    {
    }

    [Fact]
    public void Remove_ResetsGeneratedID()
    {
        var container = new TestNamingContainer();
        var child = new TestControl();
        
        container.Controls.AddWithoutPageEvents(child);
        
        Assert.True(child.PublicGenerateAutomaticID);
        Assert.NotNull(child.ID);
        var generatedId = child.ID;
        Assert.StartsWith("c", generatedId);
        
        container.Controls.Remove(child);
        
        Assert.Null(child.ID);
        Assert.Throws<InvalidOperationException>(() => child.Parent);
        Assert.True(child.Unloaded);
    }

    [Fact]
    public void Remove_DoesNotResetManualID()
    {
        var container = new TestNamingContainer();
        var child = new TestControl { ID = "manualId" };
        
        container.Controls.AddWithoutPageEvents(child);
        
        Assert.Equal("manualId", child.ID);
        
        container.Controls.Remove(child);
        
        Assert.Equal("manualId", child.ID);
        Assert.Throws<InvalidOperationException>(() => child.Parent);
    }
}
