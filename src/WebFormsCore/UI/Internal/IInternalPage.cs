using Microsoft.AspNetCore.Http;

namespace WebFormsCore.UI;

public interface IInternalPage : IInternalControl
{
    void SetContext(HttpContext context);
}
