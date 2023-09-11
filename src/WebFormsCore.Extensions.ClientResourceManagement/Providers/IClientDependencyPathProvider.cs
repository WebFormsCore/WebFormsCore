using System.Diagnostics.CodeAnalysis;
using WebFormsCore.UI;

namespace WebFormsCore.Providers;

public interface IClientDependencyPathProvider
{
    ValueTask<string?> GetPathAsync(Page page, IClientDependencyFile file, CancellationToken token);
}
