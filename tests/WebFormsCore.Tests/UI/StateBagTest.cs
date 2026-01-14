using WebFormsCore.UI;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class StateBagTest
{
    [Fact]
    public void AddAndRetrieve()
    {
        var bag = new StateBag(false);
        bag["key"] = "value";
        Assert.Equal("value", bag["key"]);
    }

    [Fact]
    public void Remove()
    {
        var bag = new StateBag(false);
        bag["key"] = "value";
        bag.Remove("key");
        Assert.Null(bag["key"]);
        Assert.False(bag.ContainsKey("key"));
    }

    [Fact]
    public void Enumeration()
    {
        var bag = new StateBag(false);
        bag["a"] = 1;
        bag["b"] = 2;
        
        var list = new List<KeyValuePair<string, object?>>();
        foreach (var kvp in bag)
        {
            list.Add(kvp);
        }

        Assert.Equal(2, list.Count);
        Assert.Contains(list, kvp => kvp.Key == "a" && (int?)kvp.Value == 1);
        Assert.Contains(list, kvp => kvp.Key == "b" && (int?)kvp.Value == 2);
    }

    [Fact]
    public void IDictionary_Methods()
    {
        IDictionary bag = new StateBag(true);
        bag.Add("Key", "Value");
        Assert.True(bag.Contains("key"));
        Assert.Equal("Value", bag["KEY"]);
        
        bag.Remove("key");
        Assert.False(bag.Contains("Key"));
    }

    [Fact]
    public void TryGetValue()
    {
        var bag = new StateBag(false);
        bag["a"] = 1;
        
        Assert.True(bag.TryGetValue("a", out var val));
        Assert.Equal(1, val);
        
        Assert.False(bag.TryGetValue("b", out var val2));
        Assert.Null(val2);
    }

    [Fact]
    public void KeysAndValues()
    {
        var bag = new StateBag(false);
        bag["a"] = 1;
        bag["b"] = 2;
        
        Assert.Equal(new[] { "a", "b" }, bag.Keys.OrderBy(k => k));
        Assert.Equal(new[] { 1, 2 }, bag.Values.Cast<int>().OrderBy(v => v));
    }

    [Fact]
    public void ValueCollection_Contains()
    {
        var bag = new StateBag(false);
        bag["a"] = 100;
        var values = bag.Values;
        
        Assert.Contains(100, values);
        Assert.DoesNotContain(200, values);
        Assert.Single(values);
    }

    [Fact]
    public void TrackViewState_MarksNewItemsAsDirty()
    {
        var bag = new StateBag(false);
        bag["old"] = 1;
        
        // Use reflection to call internal TrackViewState for unit testing the bag logic
        var method = typeof(StateBag).GetMethod("TrackViewState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(bag, null);
        
        bag["new"] = 2;
        
        Assert.Equal(1, bag.ViewStateCount);
        Assert.Contains(bag, kv => kv.Key == "new" && (int?)kv.Value == 2);
    }

    [Fact]
    public void TrackViewState_MarksChangedItemsAsDirty()
    {
        var bag = new StateBag(false);
        bag["key"] = 1;

        // Use reflection to call internal TrackViewState
        var method = typeof(StateBag).GetMethod("TrackViewState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(bag, null);
        
        bag["key"] = 2;
        
        Assert.Equal(1, bag.ViewStateCount);
        Assert.Equal(2, bag["key"]);
    }

    [Fact]
    public void SetNull_RemovesItem()
    {
        var bag = new StateBag(false);
        bag["key"] = "value";
        bag["key"] = null;
        
        Assert.False(bag.ContainsKey("key"));
        Assert.Empty(bag);
    }
}
