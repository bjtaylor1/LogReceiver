using System.Collections;
using System.Collections.Generic;

namespace LogReceiver

{
    public class ListDictionary<TKey, T> : IList<T>, IDictionary<TKey, T> where T : IHasKey<TKey>
    {
        private readonly List<T> list = new List<T>();
        private readonly Dictionary<TKey, T> dict = new Dictionary<TKey, T>();

        public T this[int index] { get => list[index]; set => list[index] = value; }
        public T this[TKey key] { get => dict[key]; set => dict[key] = value; }

        public int Count => dict.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => dict.Keys;

        public ICollection<T> Values => ((IDictionary<TKey, T>)dict).Values;

        public void Add(T item)
        {
            list.Add(item);
            dict.Add(item.Key, item);
        }

        public void Add(TKey key, T value)
        {
            list.Add(value);
            dict.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, T> item)
        {
            list.Add(item.Value);
            dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            list.Clear();
            dict.Clear();
        }

        public bool Contains(T item)
        {
            return dict.ContainsKey(item.Key);
        }

        public bool Contains(KeyValuePair<TKey, T> item)
        {
            return dict.ContainsKey(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(KeyValuePair<TKey, T>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, T>>)dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, T>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(0, item);
            dict.Add(item.Key, item);
        }

        public bool Remove(T item)
        {
            dict.Remove(item.Key);
            return list.Remove(item);
        }

        public bool Remove(TKey key)
        {
            bool retval = false;
            if(dict.TryGetValue(key, out var val))
            {
                list.Remove(val);
                dict.Remove(key);
                retval = true;
            }
            return retval;
        }

        public bool Remove(KeyValuePair<TKey, T> item)
        {
            list.Remove(item.Value);
            return dict.Remove(item.Key);
        }

        public void RemoveAt(int index)
        {
            var val = list[index];
            dict.Remove(val.Key);
            list.RemoveAt(index);
        }

        public bool TryGetValue(TKey key, out T value)
        {
            return dict.TryGetValue(key, out value);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
