﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class ControlCollection : IReadOnlyCollection<Control>
{
    private readonly List<Control> _list = new();
    private string? _readOnlyErrorMsg;
    private readonly List<Control> _namingContainerChildren = new();

    public ControlCollection(Control owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal void AddNamingContainerChild(Control control) => _namingContainerChildren.Add(control);

    internal void RemoveNamingContainerChild(Control control) => _namingContainerChildren.Remove(control);

    public IReadOnlyList<Control> NamingContainerChildren => _namingContainerChildren;

    public virtual void AddWithoutPageEvents(Control child)
    {
        _list.Add(child);
        Owner.AddedControlInternal(child, true);
    }

    public virtual ValueTask AddAsync(Control child)
    {
        AddWithoutPageEvents(child);

        var state = Owner._state;

        return state != ControlState.Constructed
            ? Owner.InvokeStateMethodsAsync(state, child)
            : default;
    }

    public void Swap(Control control, int newIndex)
    {
        if (!_list.Remove(control)) return;
        
        _list.Insert(newIndex, control);
        Owner.NamingContainer?.UpdateGeneratedIds();
    }

    public void MoveToLast(Control control)
    {
        if (!_list.Remove(control)) return;

        _list.Add(control);
        Owner.NamingContainer?.UpdateGeneratedIds();
    }

    public void MoveToFront(Control control)
    {
        if (!_list.Remove(control)) return;

        _list.Insert(0, control);
        Owner.NamingContainer?.UpdateGeneratedIds();
    }

    public virtual void Clear()
    {
        while (_list.Count > 0)
        {
            RemoveAt(_list.Count - 1);
        }

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

    public Control Owner { get; }

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
        Owner.RemovedControlInternal(child);
    }

    public virtual bool Remove(Control child)
    {
        if (!_list.Remove(child))
        {
            return false;
        }

        Owner.RemovedControlInternal(child);
        return true;
    }
}
