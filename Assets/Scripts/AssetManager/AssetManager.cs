using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
public class AssetManager : MonoBehaviour
{

    public enum ReleaseType
    {
        OnNewScene,
        Manual,
        NoneRelease
    }
    public static AssetManager Instance;
    public bool IsInitialize;
    private bool enableHotUpdate = false;
    public Action<int, float> progressHandler;

    public static AsyncOperationHandle defaultHandle;

    private Dictionary<AsyncOperationHandle, GameObject> handleDic = new Dictionary<AsyncOperationHandle, GameObject>();

    private void Awake()
    {
        Instance = this;
        GameObject.DontDestroyOnLoad(gameObject);
    }
    public void Initialize()
    {
        Addressables.InitializeAsync().Completed += InitialCompleted;
    }

    private void InitialCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
        if (!enableHotUpdate)
        {
            IsInitialize = true;
            return;
        }
        Addressables.CheckForCatalogUpdates(true).Completed += CheckComplete;
    }

    private void CheckComplete(AsyncOperationHandle<List<string>> obj)
    {
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError(" 检查 catalog失败");
            return;
        }
        Debug.Log(obj.Result.Count);
        if (obj.Result.Count == 0)
        {
            DownLoadSize();
            return;
        }
        Addressables.UpdateCatalogs(obj.Result).Completed += UpdateCatalogsComplete;
    }



    private void UpdateCatalogsComplete(AsyncOperationHandle<List<IResourceLocator>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        DownLoadSize(obj.Result);
    }

    long updateSize;

    public long GetDownloadSize()
    {
        return updateSize;
    }
    public void DownLoadSize(List<IResourceLocator> obj = null)
    {
        StartCoroutine(DownLoadSizeIe(obj));
    }

    public IEnumerator DownLoadSizeIe(List<IResourceLocator> obj = null)
    {
        long tempSize = 0;
        updateSize = 0;
        var locators = Addressables.ResourceLocators;
        if (obj != null)
        {
            locators = obj;
        }
        Dictionary<IResourceLocator, long> dic = new Dictionary<IResourceLocator, long>();
        float index = 0;
        int count = obj == null ? 2 : obj.Count;
        foreach (var item in locators)
        {
            var handle = Addressables.GetDownloadSizeAsync(item.Keys);
            yield return handle;
            if (handle.Result > 0)
            {
                tempSize += handle.Result;
                dic.Add(item, handle.Result);
            }
            Addressables.Release(handle);
            //if (progressHandler != null)
            //{
            //    index++;
            //    progressHandler(0, index/count);
            //}

        }
        updateSize = tempSize;
        float currentDownloadSize = 0;
        float totalDownLoadSize = 0;
        float downloadPercent = 0;
        if (updateSize > 0)
        {
            foreach (var item in dic)
            {
                Debug.Log("download " + item.Key);
                var downloadHandle = Addressables.DownloadDependenciesAsync(item.Key.Keys, Addressables.MergeMode.Union);
                while (!downloadHandle.IsDone)
                {
                    currentDownloadSize = downloadHandle.PercentComplete * item.Value;
                    downloadPercent = (totalDownLoadSize + currentDownloadSize) / updateSize;
                    if (progressHandler != null)
                    {
                        progressHandler(0, downloadPercent);
                    }
                    yield return null;
                }
                totalDownLoadSize += currentDownloadSize;
                Addressables.Release(downloadHandle);
            }
            if (progressHandler != null)
            {
                progressHandler(0, 1);
            }
            IsInitialize = true;
        }
        else
        {
            IsInitialize = true;
        }


    }

    /// <summary>
    /// 不需要加载的资源
    /// </summary>
    Dictionary<string, GameObject> staticPool = new Dictionary<string, GameObject>();

    /// <summary>
    /// 注册不需要加载的资源
    /// </summary>
    /// <param name="key"></param>
    /// <param name="obj"></param>
    public virtual void RegisterAsset(string key, GameObject obj)
    {
        if (obj!=null)
        {
            staticPool[key] = obj;
        }
       
    }

    /// <summary>
    /// 有可能卡死
    /// </summary>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    public T LoadAssetSync<T>(string key) where T : UnityEngine.Object
    {
        //if (staticPool.ContainsKey(key))
        //{
        //    return staticPool[key] as T;
        //}
        //var item = GameObjectPool.GetGameObject(key);
        //if (item != null)
        //{
        //    item.SetActive(true);
        //    return item as T;
        //}
        //var refObj = RefPool<T>.GetObject(key);
        //if (refObj != null)
        //{
        //    return refObj;
        //}
        var op = Addressables.LoadAssetAsync<T>(key);
        var res = op.WaitForCompletion();

        return res;
    }


    public AsyncOperationHandle LoadAssetAsync<T>(string key, Action<string, T> callback) where T : UnityEngine.Object
    {
        if (staticPool.ContainsKey(key))
        {
            if (callback != null)
                callback(key, staticPool[key] as T);
            return defaultHandle;
        }
        var item = GameObjectPool.GetGameObject(key);
        if (item != null)
        {
            item.SetActive(true);
            if (callback != null)
                callback(key, item as T);
            return defaultHandle;
        }

        var op = Addressables.LoadAssetAsync<T>(key);
        op.Completed += (res) =>
        {
            if (callback != null)
            {
                if (res.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = res.Result as GameObject;
                    if (go)
                    {
                        //if (EngineDelegate.PrefabUnInstantiateRule != null && EngineDelegate.PrefabUnInstantiateRule(go))
                        //{
                        //    callback(key, go as T);
                        //}
                        //else
                        {
                            handleDic[op] = UnityEngine.GameObject.Instantiate(go);
                            callback(key, handleDic[op] as T);
                        }

                    }
                    else
                    {
                        callback(key, res.Result);
                    }
                }
                else
                {
                    Debug.LogError("加载资源失败 " + key);
                    callback(key, null);
                }

            }
        };

        return op;
    }


    public void LoadSceneAsync(string key, Action<string> callback, LoadSceneMode loadSceneMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
    {
        Addressables.LoadSceneAsync(key, loadSceneMode, activateOnLoad, priority).Completed += (res) =>
        {

            if (callback != null)
            {
                callback(key);
            }

        };
    }

    public void ReleaseHandle(AsyncOperationHandle handle,bool forece=false)
    {
        if (forece)
        {
            Addressables.Release(handle);
            return;
        }
        GameObject go;
        if (handleDic.TryGetValue(handle,out go))
        {
            if (go!=null)
            {
                //加载的资源还在使用,不能释放,只能update释放
                return;
            }
            else
            {
                handleDic.Remove(handle);
            }
        }
        Addressables.Release(handle);
    }

    public void UpdateHandles()
    {
        foreach (var item in handleDic)
        {
            if (item.Value==null)
            {
                handleDic.Remove(item.Key);
                ReleaseHandle(item.Key);
                return;
            }
        }
    }
}
