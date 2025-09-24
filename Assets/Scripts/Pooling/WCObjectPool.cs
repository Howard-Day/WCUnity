using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WCObjectPool<T> : ObjectPool<T> where T : Component
{
    public WCObjectPool(Func<T> createFunc, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000) 
        : base(createFunc, Activate, Deactivate, Destroy, collectionCheck, defaultCapacity, maxSize)
    {
    }

    static void Activate(T instance)
    {
        instance.gameObject.SetActive(true);
    }

    static void Deactivate(T instance)
    {
        instance.gameObject.SetActive(false);
    }

    static void Destroy(T instance)
    {
        GameObject.Destroy(instance.gameObject);
    }
}
