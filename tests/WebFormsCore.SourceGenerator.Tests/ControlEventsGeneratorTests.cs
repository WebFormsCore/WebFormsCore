using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore;
using WebFormsCore.SourceGenerator;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class ControlEventsGeneratorTests
{
    [Fact]
    public void Generator_IsIncremental()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System;
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public class TestControl : Control
            {
                public event AsyncEventHandler<TestControl, EventArgs>? Clicked;
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);

        var driver = CreateDriver(new ControlEventsGenerator(), parseOptions, trackSteps: true);

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatorResult = runResult.Results.Single();
        var outputs = generatorResult.TrackedSteps
            .SelectMany(step => step.Value)
            .SelectMany(step => step.Outputs)
            .ToArray();

        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Fact]
    public void Generator_EmitsExpectedCode()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System;
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public class TestControl : Control
            {
                public event AsyncEventHandler<TestControl, EventArgs>? Clicked;
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new ControlEventsGenerator(), parseOptions, trackSteps: false);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(source => source.HintName == "ControlEvents.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public static partial class TestControlExtensions", generated, StringComparison.Ordinal);
        Assert.Contains("extension<T>(T control)", generated, StringComparison.Ordinal);
        Assert.Contains("where T : global::Tests.TestControl", generated, StringComparison.Ordinal);
        Assert.Contains("public global::System.Func<T, global::System.EventArgs, global::System.Threading.Tasks.Task> OnClickedAsync", generated, StringComparison.Ordinal);
        Assert.Contains("set => control.Clicked += async (sender, args) => await value((T)sender, args);", generated, StringComparison.Ordinal);
        Assert.Contains("public global::System.Action<T, global::System.EventArgs> OnClicked", generated, StringComparison.Ordinal);
        Assert.Contains("set => control.Clicked += (sender, args) => { value((T)sender, args); return global::System.Threading.Tasks.Task.CompletedTask; }", generated, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GeneratedEvents_CanAttachAndInvoke()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public class GeneratedControl : Control
            {
                public event AsyncEventHandler<GeneratedControl, EventArgs>? Clicked;

                public Task RaiseAsync()
                    => Clicked.InvokeAsync(this, EventArgs.Empty).AsTask();
            }

            public static class EventAttachRunner
            {
                public static bool WasCalled { get; private set; }

                public static async Task<bool> RunAsync()
                {
                    var control = new GeneratedControl
                    {
                        OnClicked = (sender, args) =>
                        {
                            WasCalled = true;
                        }
                    };

                    await control.RaiseAsync();
                    return WasCalled;
                }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new ControlEventsGenerator(), parseOptions, trackSteps: false);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);
        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        using var ms = new MemoryStream();
        var emitResult = updatedCompilation.Emit(ms);

        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));

        ms.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
        var runnerType = assembly.GetType("Tests.EventAttachRunner");
        Assert.NotNull(runnerType);

        var method = runnerType!.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var task = (Task<bool>)method!.Invoke(null, null)!;
        var called = await task;

        Assert.True(called);
    }

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees)
    {
        return CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                assemblies.Add(path);
            }
        }

        assemblies.Add(typeof(Control).Assembly.Location);
        assemblies.Add(typeof(AsyncEventHandler).Assembly.Location);

        return assemblies
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }

    private static GeneratorDriver CreateDriver(IIncrementalGenerator generator, CSharpParseOptions parseOptions, bool trackSteps)
    {
        var options = new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps);

        return CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(generator.AsSourceGenerator()),
            parseOptions: parseOptions,
            driverOptions: options);
    }
}