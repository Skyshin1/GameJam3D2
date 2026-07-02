using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public interface IPoolable
    {
        void OnTakenFromPool();
        void OnReturnedToPool();
    }

    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Stack<T> available = new Stack<T>();
        private readonly Func<T> factory;
        private readonly Transform poolRoot;

        public ComponentPool(Func<T> factory, Transform poolRoot, int prewarmCount)
        {
            this.factory = factory;
            this.poolRoot = poolRoot;

            for (int i = 0; i < prewarmCount; i++)
            {
                T item = CreateItem();
                item.gameObject.SetActive(false);
                available.Push(item);
            }
        }

        public T Get()
        {
            T item = available.Count > 0 ? available.Pop() : CreateItem();
            item.transform.SetParent(null, true);
            item.gameObject.SetActive(true);
            (item as IPoolable)?.OnTakenFromPool();
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf)
            {
                return;
            }

            (item as IPoolable)?.OnReturnedToPool();
            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot, false);
            available.Push(item);
        }

        private T CreateItem()
        {
            T item = factory();
            item.transform.SetParent(poolRoot, false);
            return item;
        }
    }
}
