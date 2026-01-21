using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

public class DefaultPageFactory : IPageFactory
{
    public Task<Page> CreatePageForControlAsync(HttpContext context, Control control)
    {
        var activator = context.RequestServices.GetRequiredService<IWebObjectActivator>();
        var page = activator.CreateControl<Page>();

        var doctype = activator.CreateLiteral("<!DOCTYPE html>");
        page.Controls.AddWithoutPageEvents(doctype);
        page.Controls.AddWithoutPageEvents(activator.CreateControl<HtmlHead>());

        var body = activator.CreateControl<HtmlBody>();
        body.Controls.AddWithoutPageEvents(control);
        page.Controls.AddWithoutPageEvents(body);

        return Task.FromResult(page);
    }
}
