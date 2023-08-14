namespace WebFormsCore.UI;

public interface IControlInterceptor
{
    T OnControlCreated<T>(T control) where T : Control;
}
