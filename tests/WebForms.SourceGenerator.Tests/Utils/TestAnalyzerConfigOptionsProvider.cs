using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WebForms.SourceGenerator.Tests.Utils;

public class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    {
        GlobalOptions = new DictionaryAnalyzerConfigOptions(globalOptions);
    }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    private class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public DictionaryAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value);
        }
    }
}