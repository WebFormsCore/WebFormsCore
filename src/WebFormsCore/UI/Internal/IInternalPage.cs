namespace WebFormsCore.UI;

public interface IInternalPage : IInternalControl
{
    void Initialize(IHttpContext context);
}
