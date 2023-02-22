using System;
using System.Collections;
using System.Collections.Generic;

namespace WebFormsCore.UI;

public class ControlCollection : ICollection, ICollection<Control>
{
    private readonly List<Control> _list = new();
    private string? _readOnlyErrorMsg;

    public ControlCollection(Control owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public virtual void Add(Control child)
    {
        var index = Count;
        _list.Add(child);
        Owner.AddedControl(child, index);
    }

    public virtual void AddAt(int index, Control child)
    {
        _list.Insert(index, child);
        Owner.AddedControl(child, index);
    }

    public virtual void Swap(int oldIndex, int newIndex)
    {
        (_list[oldIndex], _list[newIndex]) = (_list[newIndex], _list[oldIndex]);
    }

    public void Swap(Control control, int newIndex)
    {
        Swap(IndexOf(control), newIndex);
    }

    public void Swap(Control control1, Control control2)
    {
        Swap(IndexOf(control1), IndexOf(control2));
    }

    public void MoveToLast(int oldIndex)
    {
        Swap(oldIndex, Count - 1);
    }

    public void MoveToLast(Control control)
    {
        MoveToLast(IndexOf(control));
    }

    public virtual void Clear()
    {
        _list.Clear();

        if (Owner is INamingContainer)
        {
            Owner.ClearNamingContainer();
        }
    }


    public virtual bool Contains(Control c)
    {
        return _list.Contains(c);
    }

    public void CopyTo(Control[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public virtual int Count => _list.Count;

    protected Control Owner { get; }

    public virtual int IndexOf(Control value)
    {
        return _list.IndexOf(value);
    }

    public List<Control>.Enumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator<Control> IEnumerable<Control>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual void CopyTo(Array array, int index)
    {
        ((ICollection)_list).CopyTo(array, index);
    }

    public object SyncRoot => this;


    public bool IsReadOnly => (_readOnlyErrorMsg != null);

    // Setting an error message makes the control collection read only.  If the user tries to modify
    // the collection, we look up the error message in the resources and throw an exception.
    // Set errorMsg to null to make the collection not read only.
    internal string? SetCollectionReadOnly(string errorMsg)
    {
        var oldError = _readOnlyErrorMsg;
        _readOnlyErrorMsg = errorMsg;
        return oldError;
    }

    public bool IsSynchronized => false;

    public virtual Control this[int index] => _list[index];

    public virtual void RemoveAt(int index)
    {
        var child = _list[index];
        _list.RemoveAt(index);
        Owner.RemovedControl(child);
    }

    public virtual bool Remove(Control child)
    {
        if (!_list.Remove(child))
        {
            return false;
        }

        Owner.RemovedControl(child);
        return true;
    }
}
