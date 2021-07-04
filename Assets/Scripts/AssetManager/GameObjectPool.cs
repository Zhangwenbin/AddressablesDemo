using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class GameObjectPool 
{
    private static Dictionary<string , CacheItem> pools = new Dictionary<string, CacheItem>();//gameobject 缓存池
    public static void RecycleGameObject(string assetName,GameObject go)
    {
        CacheItem item = null;
        if (!pools.TryGetValue(assetName,out item))
        {
            item = new CacheItem();
            pools[assetName] = item;
        }
        if (!item.PutIn(go))
        {
            GameObject.Destroy(go);
        }
    }

    public static GameObject GetGameObject(string assetName)
    {
        CacheItem item = null;
        if (pools.TryGetValue(assetName, out item))
        {
            return item.Get();
        }
        return null;
    }

  

    public class CacheItem
    {
        public CacheItem()
        {
            maxCount = 8;
            array = new GameObject[maxCount];
            index = -1;
        }
        private int index;
        public int maxCount;
        public GameObject[] array;
        public bool PutIn(GameObject go)
        {
            if (index<maxCount-1)
            {
                index++;
                array[index] = go;
                go.SetActive(false);
                return true;
            }
            return false;
        }

        public GameObject Get()
        {
            if (index >=0)
            {
                index--;
                return array[index + 1];
            }
            return null;
        }
    }

}

public static class RefPool<T> where T:UnityEngine.Object
{
    private static Dictionary<string, RefItem<T>> refPools = new Dictionary<string, RefItem<T>>();//引用类型缓存池
    public static void OnLoadObject(string assetName, T obj, AsyncOperationHandle handle) 
    {
        RefItem<T> item = null;
        if (!refPools.TryGetValue(assetName, out item))
        {
            item = new RefItem<T>(obj, handle);
            item.AddRef();
            refPools[assetName] = item;
        }
    }

    public static T GetObject(string assetName)
    {
        RefItem<T> item = null;
        if (refPools.TryGetValue(assetName, out item))
        {
            item.AddRef();
            return item.obj;
        }
        return null;
    }
    public class RefItem<U> where U : UnityEngine.Object
    {
        public U obj;
        public AsyncOperationHandle handle;
        public int refNum = 0;
        public RefItem(U obj, AsyncOperationHandle handle)
        {
            this.obj = obj;
            this.handle = handle;
            refNum = 0;
        }

        public void AddRef()
        {
            refNum++;
        }

        public void SubRef()
        {
            refNum--;
        }

        public bool CanRelease()
        {
            return refNum <= 0;
        }

        public void Release()
        {
            refNum = 0;
            obj = null;
            Addressables.Release(handle);
        }
    }

}
