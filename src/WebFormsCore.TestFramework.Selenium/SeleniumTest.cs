using System.Collections;
using System.Collections.Generic;

namespace WebFormsCore;

public static class SeleniumTest
{
    public enum Browser
    {
        Chrome,
        Firefox,
    }

    public class BrowserData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [Browser.Chrome];
            yield return [Browser.Firefox];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
