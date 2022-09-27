using Microsoft.Extensions.ObjectPool;
using WebFormsCore.UI;

namespace WebFormsCore;

internal class ControlObjectPolicy<T> : PooledObjectPolicy<T>
    where T : Control, new()
{
    public override T Create()
    {
        return new T();
    }

    public override bool Return(T obj)
    {
        obj.ClearControl();
        return true;
    }
}
