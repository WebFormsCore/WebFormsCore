global using Xunit;
using System.Runtime.CompilerServices;
using VerifyTests;

internal static class Usings
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}