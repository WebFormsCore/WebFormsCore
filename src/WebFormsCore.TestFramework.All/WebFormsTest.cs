
using System.Collections;
using WebFormsCore.TestFramework.AngleSharp;
using WebFormsCore.UI;

namespace WebFormsCore.TestFramework;

public class WebFormsTest
{
    public static Task<ITestContext<T>> StartAsync<T>(TestType testType)
        where T : Page, new()
    {
        return testType switch
        {
            TestType.AngleSharp => AngleSharpTest.StartAngleSharpAsync<T>(),
            TestType.Chrome => SeleniumTest.StartChromeAsync<T>(),
            TestType.Firefox => SeleniumTest.StartFirefoxAsync<T>(),
            _ => throw new NotSupportedException(),
        };
    }

    public enum TestType
    {
        AngleSharp,
        Chrome,
        Firefox,
    }
}

public class TestTypeData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [WebFormsTest.TestType.AngleSharp];
        yield return [WebFormsTest.TestType.Chrome];
        yield return [WebFormsTest.TestType.Firefox];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}