using System;

namespace WebFormsCore.Serializer;

public class ViewStateObjectSerializer : ViewStateSerializer<IViewStateObject>
{
    public override void TrackViewState(Type type, IViewStateObject? value, ViewStateProvider provider)
    {
        value?.TrackViewState(provider);
    }

    public override bool CanSerialize(Type type)
    {
        return typeof(IViewStateObject).IsAssignableFrom(type);
    }

    public override void Write(Type type, ref ViewStateWriter writer, IViewStateObject? value, IViewStateObject? defaultValue)
    {
        value!.WriteViewState(ref writer);
    }

    public override IViewStateObject? Read(Type type, ref ViewStateReader reader, IViewStateObject? defaultValue)
    {
        var value = defaultValue ?? (IViewStateObject) Activator.CreateInstance(type)!;
        value.ReadViewState(ref reader);
        return value;
    }

    public override bool StoreInViewState(Type type, IViewStateObject? value, IViewStateObject? defaultValue)
    {
        return value is not null && value.WriteToViewState;
    }
}
