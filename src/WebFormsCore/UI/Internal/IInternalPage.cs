using HttpStack;

namespace WebFormsCore.UI;

public interface IInternalPage : IInternalControl
{
    void SetContext(IHttpContext context);
}
