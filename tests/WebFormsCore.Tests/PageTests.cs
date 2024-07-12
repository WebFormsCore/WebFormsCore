using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public partial class PageTest
{
    public sealed class TestResult : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public TestResult(ServiceProvider serviceProvider, Page page, string html)
        {
            _serviceProvider = serviceProvider;
            Page = page;
            Html = html;
        }

        public Page Page { get; }

        public string Html { get;  }

        public ValueTask DisposeAsync()
        {
            return _serviceProvider.DisposeAsync();
        }
    }

    public class TestEnvironment : IWebFormsEnvironment
    {
        public string? ContentRootPath => AppContext.BaseDirectory;

        public bool EnableControlWatcher => false;

        public bool CompileInBackground => false;
    }
}
