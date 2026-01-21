using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public interface IRef : IViewStateObject
{
    bool StoreInViewState(ref ViewStateWriter writer);
}

public sealed class Ref<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : IRef
{
    private T _defaultValue = default!;
    private bool _isTracking;

    public Ref(T value = default!)
    {
        Value = value;

        if (!typeof(Control).IsAssignableFrom(typeof(T)))
        {
            var current = RefControl.Current.Value;

            if (current == null)
            {
                throw new InvalidOperationException("Ref<T> can only be used within a RefControl initializer when T is not a Control.");
            }

            current.RegisterRef(this);
        }
    }

    public T Value
    {
        get;
        set
        {
            field = value;

            if (!_isTracking)
            {
                _defaultValue = value;
            }
        }
    }

    bool IViewStateObject.WriteToViewState => true;

    void IViewStateObject.TrackViewState(ViewStateProvider provider)
    {
        if (_isTracking) return;

        _isTracking = true;
        _defaultValue = Value;
    }

    void IViewStateObject.WriteViewState(ref ViewStateWriter writer)
    {
        writer.Write(Value);
    }

    void IViewStateObject.ReadViewState(ref ViewStateReader reader)
    {
        _isTracking = true;
        Value = reader.Read<T>()!;
    }

    bool IRef.StoreInViewState(ref ViewStateWriter writer)
    {
        return writer.StoreInViewState(Value, _defaultValue);
    }

    public override string? ToString()
    {
        return Value?.ToString();
    }
}

public interface IRefControl
{
    void RegisterRef(IRef viewStateObject);
}

public readonly struct RefScope : IDisposable
{
    private readonly IRefControl? _previous;

    public RefScope(IRefControl control)
    {
        _previous = RefControl.Current.Value;
        RefControl.Current.Value = control;
    }

    public void Dispose()
    {
        RefControl.Current.Value = _previous;
    }
}

public class FuncRefControl(Func<Control, Task> initializer) : RefControl
{
    protected override async ValueTask OnPreInitAsync(CancellationToken token)
    {
        await base.OnPreInitAsync(token);

        using (new RefScope(this))
        {
            await initializer(this);
        }
    }
}

public abstract class RefControl : Control, IRefControl
{
    internal static AsyncLocal<IRefControl?> Current { get; } = new();

    private readonly List<IRef> _refs = new();

    protected override void TrackViewState(ViewStateProvider provider)
    {
        base.TrackViewState(provider);

        if (_refs.Count == 0)
        {
            return;
        }

        foreach (var viewStateObject in _refs)
        {
            viewStateObject.TrackViewState(provider);
        }
    }

    protected override void OnWriteViewState(ref ViewStateWriter writer)
    {
        base.OnWriteViewState(ref writer);

        if (_refs.Count == 0)
        {
            return;
        }

        ulong bits = 0;

        for (var i = 0; i < _refs.Count; i++)
        {
            var viewStateObject = _refs[i];

            if (viewStateObject.StoreInViewState(ref writer))
            {
                bits |= 1UL << i;
            }
        }

        switch (_refs.Count)
        {
            case <= 8:
                writer.Write((byte)bits);
                break;
            case <= 16:
                writer.Write((ushort)bits);
                break;
            case <= 32:
                writer.Write((uint)bits);
                break;
            default:
                writer.Write(bits);
                break;
        }

        for (var i = 0; i < _refs.Count; i++)
        {
            if ((bits & (1UL << i)) != 0)
            {
                _refs[i].WriteViewState(ref writer);
            }
        }
    }

    protected override void OnLoadViewState(ref ViewStateReader reader)
    {
        base.OnLoadViewState(ref reader);

        if (_refs.Count == 0)
        {
            return;
        }

        var bits = _refs.Count switch
        {
            <= 8 => reader.ReadByte(),
            <= 16 => reader.Read<ushort>(),
            <= 32 => reader.Read<uint>(),
            _ => reader.Read<ulong>()
        };

        for (var i = 0; i < _refs.Count; i++)
        {
            if ((bits & (1UL << i)) != 0)
            {
                _refs[i].ReadViewState(ref reader);
            }
        }
    }

    public void RegisterRef(IRef viewStateObject)
    {
        _refs.Add(viewStateObject);
    }
}

public static class ControlEventExtensions
{
    extension<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(T control) where T : Control
    {
        public Ref<T> Ref
        {
            set => value.Value = control;
        }
    }
}
