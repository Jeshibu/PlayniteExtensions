using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace XboxMetadata.Tests
{
    class FakeItemCollection<T> : IItemCollection<T> where T : DatabaseObject
    {
        public T this[Guid id] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public GameDatabaseCollection CollectionType => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public event EventHandler<ItemCollectionChangedEventArgs<T>> ItemCollectionChanged;
        public event EventHandler<ItemUpdatedEventArgs<T>> ItemUpdated;

        public T Add(string itemName)
        {
            throw new NotImplementedException();
        }

        public T Add(string itemName, Func<T, string, bool> existingComparer)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Add(List<string> items)
        {
            throw new NotImplementedException();
        }

        public T Add(MetadataProperty property)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Add(IEnumerable<MetadataProperty> properties)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Add(List<string> items, Func<T, string, bool> existingComparer)
        {
            throw new NotImplementedException();
        }

        public void Add(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void BeginBufferUpdate()
        {
            throw new NotImplementedException();
        }

        public IDisposable BufferedUpdate()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsItem(Guid id)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EndBufferUpdate()
        {
            throw new NotImplementedException();
        }

        public T Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public List<T> Get(IList<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetClone()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new List<T>().GetEnumerator();
        }

        public bool Remove(Guid id)
        {
            throw new NotImplementedException();
        }

        public bool Remove(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void Update(T item)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
