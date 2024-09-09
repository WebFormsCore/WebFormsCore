using System;
using Microsoft.AspNetCore.Http.Features;

namespace WebFormsCore.Features;

public interface ITestContextFeature
{
    ITestContext TestContext { get; }
}

public sealed class TestContextFeature(ITestContext testContext) : ITestContextFeature
{
    public ITestContext TestContext { get; } = testContext;
}
