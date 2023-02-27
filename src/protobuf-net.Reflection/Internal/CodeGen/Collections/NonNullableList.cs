#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen.Collections;

/// <summary>
/// List, which does not allow to store null items
/// </summary>
internal class NonNullableList<T> : IList<T>
{
    private readonly IList<T> _listImplementation = new List<T>();
    
    public IEnumerator<T> GetEnumerator()
    {
        return _listImplementation.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_listImplementation).GetEnumerator();
    }
    
    public void Add(T item)
    {
        if (item is null) return;
        _listImplementation.Add(item);
    }

    public void Clear()
    {
        _listImplementation.Clear();
    }

    public bool Contains(T item)
    {
        return _listImplementation.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _listImplementation.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _listImplementation.Remove(item);
    }

    public int Count => _listImplementation.Count;

    public bool IsReadOnly => _listImplementation.IsReadOnly;

    public int IndexOf(T item)
    {
        return _listImplementation.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        _listImplementation.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _listImplementation.RemoveAt(index);
    }

    public T this[int index]
    {
        get => _listImplementation[index];
        set => _listImplementation[index] = value;
    }
}