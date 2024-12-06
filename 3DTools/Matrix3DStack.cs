using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Matrix3DStack : IEnumerable<Matrix3D>, ICollection, IEnumerable
{
    public Matrix3D Peek()
    {
        return this._storage[^1];
    }

    public void Push(Matrix3D item)
    {
        this._storage.Add(item);
    }

    public void Append(Matrix3D item)
    {
        if (this.Count > 0)
        {
            Matrix3D item2 = this.Peek();
            item2.Append(item);
            this.Push(item2);
            return;
        }
        this.Push(item);
    }

    public void Prepend(Matrix3D item)
    {
        if (this.Count > 0)
        {
            Matrix3D item2 = this.Peek();
            item2.Prepend(item);
            this.Push(item2);
            return;
        }
        this.Push(item);
    }

    public Matrix3D Pop()
    {
        Matrix3D result = this.Peek();
        this._storage.RemoveAt(this._storage.Count - 1);
        return result;
    }

        public int Count => this._storage.Count;

    private void Clear()
    {
        this._storage.Clear();
    }

    private bool Contains(Matrix3D item)
    {
        return this._storage.Contains(item);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)this._storage).CopyTo(array, index);
    }

        bool ICollection.IsSynchronized
    {
        get
        {
            return ((ICollection)this._storage).IsSynchronized;
        }
    }

        object ICollection.SyncRoot
    {
        get
        {
            return ((ICollection)this._storage).SyncRoot;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<Matrix3D>)this).GetEnumerator();
    }

    IEnumerator<Matrix3D> IEnumerable<Matrix3D>.GetEnumerator()
    {
        for (int i = this._storage.Count - 1; i >= 0; i--)
        {
            yield return this._storage[i];
        }
        yield break;
    }

    private readonly List<Matrix3D> _storage = [];
}
