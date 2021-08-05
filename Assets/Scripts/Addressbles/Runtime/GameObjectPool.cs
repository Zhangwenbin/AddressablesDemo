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
                GameObject.DontDestroyOnLoad(go);
                return true;
            }
            return false;
        }

        public GameObject Get()
        {
            while (index >=0)
            {
                index--;
                if (array[index+1]!=null)
                {
                    return array[index + 1];
                }
            }
            return null;
        }
    }

}

public static class RefPool
{
    private static Dictionary<string, RefItem> refPools = new Dictionary<string, RefItem>();//引用类型缓存池
    public static void OnLoadObject(string assetName, AsyncOperationHandle handle) 
    {
        lock (refPools)
        {
            RefItem item = null;
            if (!refPools.TryGetValue(assetName, out item))
            {
                item = new RefItem(handle);
                item.AddRef();
                refPools.Add(assetName, item);
            }
        }

    }

    public static Object GetObject(string assetName)
    {
        RefItem item = null;
        if (refPools.TryGetValue(assetName, out item))
        {
            item.AddRef();
            return item.handle.Result as Object;
        }
        return null;
    }

    public static void ReleaseObject(string assetName)
    {
        RefItem item = null;
        if (refPools.TryGetValue(assetName, out item))
        {
            item.SubRef();
            if (item.CanRelease())
            {
                item.Release();
                refPools.Remove(assetName);
            }
        }
    }
    public class RefItem
    {
        public AsyncOperationHandle handle;
        public int refNum = 0;
        public RefItem(AsyncOperationHandle handle)
        {
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
            Addressables.Release(handle);
        }
    }

}
