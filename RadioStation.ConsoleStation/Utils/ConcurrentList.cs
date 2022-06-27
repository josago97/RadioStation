using System;
using System.Collections;
using System.Collections.Generic;

namespace RadioStation.ConsoleStation.Utils
{
    public class ConcurrentList<T> : IList<T>
    {
        private readonly List<T> _list;

        public ConcurrentList()
        {
            _list = new List<T>();
        }

        public ConcurrentList(IEnumerable<T> collection)
        {
            _list = new List<T>(collection);
        }

        public ConcurrentList(int capacity)
        {
            _list = new List<T>(capacity);
        }

        public T this[int index]
        {
            get { lock (_list) return _list[index]; }
            set { lock (_list) _list[index] = value; }
        }

        public int Count
        {
            get { lock (_list) return _list.Count; }
        }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (_list) _list.Add(item);
        }

        public void Clear()
        {
            lock (_list) _list.Clear();
        }

        public bool Contains(T item)
        {
            lock (_list) return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_list) _list.CopyTo(array, arrayIndex);
        }

        public void ForEach(Action<T> action)
        {
            lock (_list) _list.ForEach(action);
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_list) return Clone().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            lock (_list) return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (_list) _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            lock (_list) return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            lock (_list) _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IList<T> Clone()
        {
            return new List<T>(_list);
        }
    }
}
