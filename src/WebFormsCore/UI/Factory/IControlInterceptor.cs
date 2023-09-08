namespace WebFormsCore.UI;

public interface IControlInterceptor<T>
{
    T OnControlCreated(T control);
}
