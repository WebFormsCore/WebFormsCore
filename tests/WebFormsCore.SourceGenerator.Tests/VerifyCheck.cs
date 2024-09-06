using System.Threading.Tasks;
using VerifyXunit;

namespace WebFormsCore.SourceGenerator.Tests;

public class VerifyChecksTests
{
    [Fact]
    public Task Run() => VerifyChecks.Run();
}