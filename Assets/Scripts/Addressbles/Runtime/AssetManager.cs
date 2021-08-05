using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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

    public static AsyncOperationHandle defaultHandle;

    private Dictionary<AsyncOperationHandle, GameObject> handleDic = new Dictionary<AsyncOperationHandle, GameObject>();

    private Dictionary<string, List<LoadRequest>> cancelRequests = new Dictionary<string, List<LoadRequest>>();
    private List<LoadRequest> comingRequests = new List<LoadRequest>();
    private List<LoadRequest> loadingRequests = new List<LoadRequest>();
    private object lockObj = new object();
    private int maxLoadingCount = 10;

    private void Awake()
    {
        Instance = this;
        GameObject.DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        StartCoroutine(InitializeAsync());
    }

    IEnumerator InitializeAsync()
    {
        var inithandle = Addressables.InitializeAsync();
        inithandle.Completed += InitialCompleted;
        LoadingScreen.SetTipsStatic(1);
        while (!inithandle.IsDone)
        {
            LoadingScreen.SetProgress(inithandle.PercentComplete);
            yield return null;
        }
        yield return null;
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
        LoadingScreen.SetTipsStatic(3);
        if (updateSize > 0)
        {
            foreach (var item in dic)
            {
                Debug.Log("download " + item.Key);
                var downloadHandle =
                    Addressables.DownloadDependenciesAsync(item.Key.Keys, Addressables.MergeMode.Union);
                while (!downloadHandle.IsDone)
                {
                    currentDownloadSize = downloadHandle.PercentComplete * item.Value;
                    downloadPercent = (totalDownLoadSize + currentDownloadSize) / updateSize;
                    LoadingScreen.SetProgress(downloadPercent);
                    yield return null;
                }

                totalDownLoadSize += currentDownloadSize;
                Addressables.Release(downloadHandle);
            }

            LoadingScreen.SetProgress(1);
            IsInitialize = true;
        }
        else
        {
            IsInitialize = true;
        }
    }

    public void PreLoadAssets(Action callback, bool showProgress = false)
    {
        LoadAssetAsync("preload.bytes").callback= (preloadkey, res) =>
        {
            if (res == null)
            {
                if (callback != null)
                {
                    callback();
                }

                return;
            }

            maxLoadingCount = 50;
            var preloadasset = res as TextAsset;
            string filelist = preloadasset.text;
            string[] files = filelist.Split('\n');
            int totalCount = files.Length;
            if (showProgress)
            {
                LoadingScreen.SetTipsStatic(6);
            }

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i].Trim();
                if (string.IsNullOrEmpty(file))
                {
                    --totalCount;
                    continue;
                }

                if (file.EndsWith(".bytes"))
                {
                    LoadAssetAsyncQueue(file).callback= (key, asset) =>
                    {
                        --totalCount;
                        if (showProgress)
                        {
                            LoadingScreen.SetProgress(1 - totalCount * 1.0f / files.Length);
                        }

                        if (totalCount == 0)
                        {
                            if (callback != null)
                            {
                                callback();
                            }

                            maxLoadingCount = 10;
                        }
                    };
                }
                else if (file.EndsWith(".prefab"))
                {
                    LoadAssetAsyncQueue(file).callback= (key, resObj) =>
                    {
                        --totalCount;
                        if (showProgress)
                        {
                            LoadingScreen.SetProgress(1 - totalCount * 1.0f / files.Length);
                        }

                        var asset = resObj as GameObject;
                        if (asset!=null)
                        {
                            GameObjectPool.RecycleGameObject(key, asset);
                            if (totalCount == 0)
                            {
                                if (callback != null)
                                {
                                    callback();
                                }

                                maxLoadingCount = 10;
                            }
                        }
                    };
                }
            }
        };
    }

    /// <summary>
    /// 不需要加载的资源
    /// </summary>
    public  Dictionary<string, GameObject> staticPool = new Dictionary<string, GameObject>();
    
    

    /// <summary>
    /// 注册不需要加载的资源
    /// </summary>
    /// <param name="key"></param>
    /// <param name="obj"></param>
    public virtual void RegisterAsset(string key, GameObject obj)
    {
        if (obj != null)
        {
            staticPool[key] = obj;
        }
    }

    

    public LoadRequest LoadAssetAsyncQueue(string key, bool isSprite = false)
    {
        var req = new LoadRequest(key);
        req.isSprite = isSprite;
        comingRequests.Add(req);
        return req;
    }

    private void UpdateLoadQueue()
    {
        var count = comingRequests.Count;
            var loadingCount = loadingRequests.Count;
            while (loadingCount < maxLoadingCount && count > 0)
            {
                var req = comingRequests[0];
                req.Load(this);
                comingRequests.Remove(req);
                loadingRequests.Add(req);
                count = comingRequests.Count;
                loadingCount = loadingRequests.Count;
            }

            while (loadingCount > 0)
            {
                var loadingReq = loadingRequests[0];
                if (RemoveCancel(loadingReq.key, loadingReq.callback))
                {
                    loadingRequests.Remove(loadingReq);
                    loadingCount = loadingRequests.Count;
                    switch (loadingReq.aasetFrom)
                    {
                        case LoadRequest.AssetFrom.None:
                            ReleaseHandle(loadingReq.handle,true);
                            break;
                        case LoadRequest.AssetFrom.GameobjectPool:
                            GameObjectPool.RecycleGameObject(loadingReq.key,loadingReq.Result as GameObject);
                            break;
                        case LoadRequest.AssetFrom.RefPool:
                            RefPool.ReleaseObject(loadingReq.key);
                            break;
                        case LoadRequest.AssetFrom.StaticPool:
                            (loadingReq.Result as GameObject).SetActive(false);
                            break;
                        case LoadRequest.AssetFrom.LoadGameobjectInstantiate:
                            GameObjectPool.RecycleGameObject(loadingReq.key,loadingReq.Result as GameObject);
                            break;
                        case LoadRequest.AssetFrom.LoadGameobjectNotInstantiate:
                            ReleaseHandle(loadingReq.handle,true);
                            break;
                        case LoadRequest.AssetFrom.LoadRefAsset:
                            RefPool.ReleaseObject(loadingReq.key);
                            break;
                        default:
                            break;
                    }
                    continue;
                }

                if (loadingReq.IsDone)
                {
                    if (!loadingReq.useCache)
                    {
                        if (loadingReq.Status == AsyncOperationStatus.Succeeded)
                        {
                            GameObject go = loadingReq.handle.Result as GameObject;
                            if (go)
                            {
                                if ( PrefabUnInstantiateRule(go))
                                {
                                    loadingReq.Result = go;
                                    loadingReq.aasetFrom =LoadRequest.AssetFrom.LoadGameobjectNotInstantiate;
                                }
                                else
                                {
                                    var instance = UnityEngine.GameObject.Instantiate(go);
                                    loadingReq.Result = instance;
                                    handleDic[loadingReq.handle] = instance;
                                    loadingReq.aasetFrom =LoadRequest.AssetFrom.LoadGameobjectInstantiate;
                                }
                                
                            }
                            else
                            {
                                loadingReq.aasetFrom =LoadRequest.AssetFrom.LoadRefAsset;
                                RefPool.OnLoadObject(loadingReq.key, loadingReq.handle);
                                loadingReq.Result = loadingReq.handle.Result as Object;
                            }
                        }
                        else
                        {
                            Debug.LogError("加载资源失败 " + loadingReq.key);
                        }
                    }

                    loadingReq.CallBack();
                    loadingRequests.Remove(loadingReq);
                    loadingCount = loadingRequests.Count;
                   continue;
                }

                {
                    loadingReq.IsDone = loadingReq.handle.IsDone;
                    loadingReq.IsValid = loadingReq.handle.IsValid();
                    loadingReq.Status = loadingReq.handle.Status;
                    loadingReq.PercentComplete = loadingReq.handle.PercentComplete;
                }
                break;
               
            }
        
    }

    private bool PrefabUnInstantiateRule(GameObject obj)
    {
        return false;
    }

    public LoadRequest LoadAssetAsync(string key, bool isSprite = false, LoadRequest req = null)
    {
        if (req == null)
        {
            req = new LoadRequest(key);
        }

        req.isSprite = isSprite;
        lock (loadingRequests)
        {
            loadingRequests.Insert(0,req);
        }
        
        req.Load(this);
        return req;
    }


    public void LoadSceneAsync(string key, Action<string> callback, LoadSceneMode loadSceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true, int priority = 100)
    {
        Addressables.LoadSceneAsync(key, loadSceneMode, activateOnLoad, priority).Completed += (res) =>
        {
            if (callback != null)
            {
                callback(key);
            }
        };
    }

    public void ReleaseHandle(AsyncOperationHandle handle, bool forece = false)
    {
        if (handle.Equals(defaultHandle))
        {
            return;
        }

        if (forece)
        {
            Addressables.Release(handle);
            return;
        }

        GameObject go;
        if (handleDic.TryGetValue(handle, out go))
        {
            if (go != null)
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


    public void ReleaseRefAsset(string key)
    {
        RefPool.ReleaseObject(key);
    }

    public void UpdateHandles()
    {
        foreach (var item in handleDic)
        {
            if (item.Value == null)
            {
                handleDic.Remove(item.Key);
                ReleaseHandle(item.Key);
                return;
            }
        }
    }

    public void ReleaseCallBack(string key, Action<string, UnityEngine.Object> callback)
    {
        for (int i = 0; i < comingRequests.Count; i++)
        {
            var req = comingRequests[i];
            if (req.key==key&&req.callback.Equals(callback))
            {
                comingRequests.Remove(req);
                return;
            }
        }

        var cancelReq= new LoadRequest(key);
        cancelReq.callback = callback;
        if (cancelRequests.TryGetValue(key, out List<LoadRequest> list))
        {
            list.Add(cancelReq);
        }
        else
        {
            list = new List<LoadRequest>();
            list.Add(cancelReq);
            cancelRequests.Add(key, list);
        }
    }

    private bool RemoveCancel(string key, Action<string, UnityEngine.Object> callback)
    {
        if (cancelRequests.TryGetValue(key, out List<LoadRequest> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                var req = list[i];
                if (req.callback.Equals(callback))
                {
                    list.Remove(req);
                    return true;
                }
            }
        }

        return false;
    }

    private void Update()
    {
        UpdateHandles();
        UpdateLoadQueue();
    }
}

public class LoadRequest : IEnumerator
{
    public enum AssetFrom
    {
        None,
        StaticPool,
        GameobjectPool,
        RefPool,
        LoadGameobjectNotInstantiate,
        LoadGameobjectInstantiate,
        LoadRefAsset
    }
    public string key;
    public bool isSprite;
    public bool useCache;
    
    public  AssetFrom aasetFrom;
    /// <summary>
    /// for load
    /// </summary>
    public AsyncOperationHandle handle;

    public void Load(AssetManager rgr)
    {
        aasetFrom = AssetFrom.None;
        if (rgr.staticPool.ContainsKey(key))
        {
            this.useCache = true;
            this.IsDone = true;
            this.IsValid = true;
            this.Status = AsyncOperationStatus.Succeeded;
            this.PercentComplete = 1;
            this.Result = rgr.staticPool[key];
            aasetFrom = AssetFrom.StaticPool;
            return ;
        }

        var item = GameObjectPool.GetGameObject(key);
        if (item != null)
        {
            item.SetActive(true);
            this.useCache = true;
            this.IsDone = true;
            this.IsValid = true;
            this.Status = AsyncOperationStatus.Succeeded;
            this.PercentComplete = 1;
            this.Result = item;
            aasetFrom = AssetFrom.GameobjectPool;
            return ;
        }
        
        var refItem = RefPool.GetObject(key);
        if (refItem != null)
        {
            this.useCache = true;
            this.IsDone = true;
            this.IsValid = true;
            this.Status = AsyncOperationStatus.Succeeded;
            this.PercentComplete = 1;
            this.Result = refItem;
            aasetFrom = AssetFrom.RefPool;
            return ;
        }
        useCache = false;
        if (isSprite)
        {
            handle = Addressables.LoadAssetAsync<Sprite>(key);
        }
        else
        {
            handle = Addressables.LoadAssetAsync<Object>(key);
        }
    }

    /// <summary>
    /// True if the operation is complete.
    /// </summary>
    public bool IsDone;


    /// <summary>
    /// Check if the internal operation is not null and has the same version of this handle.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool IsValid;


    /// <summary>
    /// The progress of the internal operation.
    /// This is evenly weighted between all sub-operations. For example, a LoadAssetAsync call could potentially
    /// be chained with InitializeAsync and have multiple dependent operations that download and load content.
    /// In that scenario, PercentComplete would reflect how far the overal operation was, and would not accurately
    /// represent just percent downloaded or percent loaded into memory.
    /// For accurate download percentages, use GetDownloadStatus(). 
    /// </summary>
    public float PercentComplete;


    /// <summary>
    /// The result object of the operations.
    /// </summary>
    public Object Result;


    /// <summary>
    /// The status of the internal operation.
    /// </summary>
    public AsyncOperationStatus Status;


    object IEnumerator.Current
    {
        get { return Result; }
    }

    /// <summary>
    /// Overload for <see cref="IEnumerator.MoveNext"/>.
    /// </summary>
    /// <returns>Returns true if the enumerator can advance to the next element in the collectin. Returns false otherwise.</returns>
    bool IEnumerator.MoveNext()
    {
        return !IsDone;
    }

    /// <summary>
    /// Overload for <see cref="IEnumerator.Reset"/>.
    /// </summary>
    void IEnumerator.Reset()
    {
    }

    public Action<string, UnityEngine.Object> callback;

    public void CallBack()
    {
        if (callback != null)
        {
            callback(key, Result);
        }
    }

    public LoadRequest(string key)
    {
        this.key = key;
    }
}