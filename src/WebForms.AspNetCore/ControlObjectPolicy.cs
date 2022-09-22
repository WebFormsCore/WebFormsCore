using System.Web.UI;
using Microsoft.Extensions.ObjectPool;

namespace WebForms.AspNetCore;

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