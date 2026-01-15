using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

/// <summary>
/// Options for configuring a Selenium test fixture.
/// </summary>
public class SeleniumFixtureOptions
{
    /// <summary>
    /// Gets or sets the action to configure services.
    /// </summary>
    public Action<IServiceCollection>? Configure { get; set; }

    /// <summary>
    /// Gets or sets the action to configure the application.
    /// </summary>
    public Action<IApplicationBuilder>? ConfigureApp { get; set; }

    /// <summary>
    /// Gets or sets whether to run the browser in headless mode.
    /// When null, defaults to true unless a debugger is attached.
    /// </summary>
    public bool? Headless { get; set; }

    /// <summary>
    /// Gets or sets whether to enable view state.
    /// </summary>
    public bool EnableViewState { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP protocols to use.
    /// </summary>
    public HttpProtocols Protocols { get; set; } = HttpProtocols.Http1AndHttp2;

    /// <summary>
    /// Gets or sets whether to enable WebSockets middleware.
    /// </summary>
    public bool EnableWebSockets { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable WebFormsCore middleware.
    /// </summary>
    public bool EnableWebFormsCoreMiddleware { get; set; } = true;
}
