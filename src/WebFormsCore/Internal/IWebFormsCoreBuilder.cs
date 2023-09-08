using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public interface IWebFormsCoreBuilder
{
    IServiceCollection Services { get; }
}