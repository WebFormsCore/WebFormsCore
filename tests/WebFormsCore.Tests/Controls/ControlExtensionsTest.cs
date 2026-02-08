using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public class ControlExtensionsTest
{
	[Fact]
	public void EnumerateControlsTest()
	{
		// Root
		var root = new Control();

		// C1
		var c1 = new Control();
		root.Controls.AddWithoutPageEvents(c1);

		var c1_c1 = new Control();
		c1.Controls.AddWithoutPageEvents(c1_c1);

		var c1_c2 = new Control();
		c1.Controls.AddWithoutPageEvents(c1_c2);

		// C2
		var c2 = new Control();
		root.Controls.AddWithoutPageEvents(c2);

		var c2_c1 = new Control();
		c2.Controls.AddWithoutPageEvents(c2_c1);

		var c2_c2 = new Control();
		c2.Controls.AddWithoutPageEvents(c2_c2);

		using var enumerator = root.EnumerateSelfAndChildControls().GetEnumerator();

		Assert.True(enumerator.MoveNext());
		Assert.Equal(root, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c1, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c1_c1, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c1_c2, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c2, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c2_c1, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c2_c2, enumerator.Current);

		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void EnumerateControlsWithFilterTest()
	{
		// Root
		var root = new Control { ID = "root" };

		// C1
		var c1 = new Control { ID = "c1" };
		root.Controls.AddWithoutPageEvents(c1);

		var c1_c1 = new Control { ID = "c1_c1" };
		c1.Controls.AddWithoutPageEvents(c1_c1);

		var c1_c2 = new Control { ID = "c1_c2" };
		c1.Controls.AddWithoutPageEvents(c1_c2);

		// C2
		var c2 = new Control { ID = "c2" };
		root.Controls.AddWithoutPageEvents(c2);

		var c2_c1 = new Control { ID = "c2_c1" };
		c2.Controls.AddWithoutPageEvents(c2_c1);

		var c2_c2 = new Control { ID = "c2_c2" };
		c2.Controls.AddWithoutPageEvents(c2_c2);

		using var enumerator = root
			.EnumerateSelfAndChildControls(filter: static c => c.ID != null && c.ID.EndsWith("c1", StringComparison.Ordinal))
			.GetEnumerator();

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c1, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c1_c1, enumerator.Current);

		Assert.True(enumerator.MoveNext());
		Assert.Equal(c2_c1, enumerator.Current);

		Assert.False(enumerator.MoveNext());
	}
}
