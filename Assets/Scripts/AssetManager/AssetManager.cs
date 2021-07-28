using MUEngine;
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

    public static AsyncOperationHandle defaultHandle;

    private Dictionary<AsyncOperationHandle, GameObject> handleDic = new Dictionary<AsyncOperationHandle, GameObject>();

    private Dictionary<string, List<LoadRequest>> cancelRequests = new Dictionary<string, List<LoadRequest>>();
    private List<LoadRequest> loadQueueRequests = new List<LoadRequest>();
    private int maxLoadingCount = 10;
    private int currentLoadingCount = 0;
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
        //LoadingScreen.SetTipsStatic(1);
        while (!inithandle.IsDone)
        {
            //LoadingScreen.SetProgress(inithandle.PercentComplete);
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


        }
        updateSize = tempSize;
        float currentDownloadSize = 0;
        float totalDownLoadSize = 0;
        float downloadPercent = 0;
        //LoadingScreen.SetTipsStatic(3);
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
                    //LoadingScreen.SetProgress(downloadPercent);
                    yield return null;
                }
                totalDownLoadSize += currentDownloadSize;
                Addressables.Release(downloadHandle);
            }
            //LoadingScreen.SetProgress(1);
            IsInitialize = true;
        }
        else
        {
            IsInitialize = true;
        }


    }

    public void PreLoadAssets(Action callback,bool showProgress=false)
    {
        LoadAssetAsync<TextAsset>("preload.bytes",(preloadkey,res)=> {
            if (res==null)
            {
                if (callback!=null)
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
                //LoadingScreen.SetTipsStatic(6);
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
                    LoadAssetAsyncQueue(file, (key, asset) => {
                        --totalCount;
                        if (showProgress)
                        {
                            //LoadingScreen.SetProgress(1 - totalCount * 1.0f / files.Length);
                        }
                       
                        if (totalCount == 0)
                        {
                            if (callback != null)
                            {
                                callback();
                            }
                            maxLoadingCount = 10;
                        }
                    });
                }
                else if (file.EndsWith(".prefab"))
                {
                    LoadAssetAsyncQueue(file, (key, resObj) => {
                        --totalCount;
                        if (showProgress)
                        {
                            //LoadingScreen.SetProgress(1 - totalCount * 1.0f / files.Length);
                        }
                        var asset = resObj as GameObject;
                        if (asset is GameObject)
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

                    });
                }
                              
            }
        });
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

        //.bytes 文本资源 直接卸载,不操作
        if (key.EndsWith(".bytes"))
        {
            var txt = Addressables.LoadAssetAsync<T>(key);
            var bytes = txt.WaitForCompletion();
            ReleaseHandle(txt, true);
            return bytes;
        }

        T res;
        if (staticPool.ContainsKey(key))
        {
            return staticPool[key] as T;
        }
        var item = GameObjectPool.GetGameObject(key);
        if (item != null)
        {
             res = item as T;
            if (res != null)
            {
                item.SetActive(true);
                return res;
            }
            else
            {
                GameObjectPool.RecycleGameObject(key, item);
            }

        }

        var refItem = RefPool.GetObject(key);
        if (refItem != null)
        {
             res = refItem as T;
            if (res != null)
            {
                return res;
            }
            else
            {
                RefPool.ReleaseObject(key);
            }

        }
        var op = Addressables.LoadAssetAsync<T>(key);
          op.WaitForCompletion();
        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject go = op.Result as GameObject;
            if (go)
            {
                if (PrefabUnInstantiateRule(go))
                {
                   return go as T;
                }
                else
                {
                    handleDic[op] = UnityEngine.GameObject.Instantiate(go);
                    return handleDic[op] as T;
                }

            }
            else
            {
                RefPool.OnLoadObject(key, op);
                return op.Result;
            }
        }
        else
        {
            Debug.LogError("加载资源失败 " + key);
            return null;
        }
    }

    private bool PrefabUnInstantiateRule(GameObject go)
    {
        return true;
    }


    public void LoadAssetAsyncQueue(string key, Action<string, UnityEngine.Object> callback)
    {
        loadQueueRequests.Add(new LoadRequest(key, callback));
    }

    private void UpdateLoadQueue()
    {
        var count = loadQueueRequests.Count;
        if (count==0)
        {
            return;
        }
        while (currentLoadingCount<maxLoadingCount&&count>0)
        {
            var req = loadQueueRequests[0];
            LoadAssetAsync<UnityEngine.Object>(req.key, req.callback);
            loadQueueRequests.RemoveAt(0);
            count = loadQueueRequests.Count;
        }
    }


    public void LoadAssetAsync<T>(string key, Action<string, UnityEngine.Object> callback) where T : UnityEngine.Object
    {
        if (RemoveCancel(key,callback))
        {
            return;
        }
        if (staticPool.ContainsKey(key))
        {
            if (callback != null)
                NextFrameCallBack(callback, key, staticPool[key] as T);
            return ;
        }
        var item = GameObjectPool.GetGameObject(key);
        if (item != null)
        {
            var res = item as T;
            if (res!=null)
            {
                item.SetActive(true);
                if (callback != null)
                    NextFrameCallBack(callback, key, res);
                return;
            }
            else
            {
                GameObjectPool.RecycleGameObject(key,item);
            }

        }

        var refItem = RefPool.GetObject(key);
        if (refItem!=null)
        {
            var res = refItem as T;
            if (res !=null)
            {
                if (callback != null)
                    NextFrameCallBack(callback, key, res);
                return;
            }
            else
            {
                RefPool.ReleaseObject(key);
            }

        }
        currentLoadingCount++;
        var op = Addressables.LoadAssetAsync<T>(key);
        op.Completed += (res) =>
        {
            if (callback != null&&!RemoveCancel(key,callback ))
            {
                if (res.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = res.Result as GameObject;
                    if (go)
                    {
                        if (PrefabUnInstantiateRule(go))
                        {
                            callback(key, go as T);
                        }
                        else
                        {
                            handleDic[op] = UnityEngine.GameObject.Instantiate(go);
                            callback(key, handleDic[op] as T);
                        }

                    }
                    else
                    {
                        RefPool.OnLoadObject(key, op);
                        callback(key, res.Result);
                    }
                }
                else
                {
                    Debug.LogError("加载资源失败 " + key);
                    callback(key, null);
                }

            }
            currentLoadingCount--;
        };

        return ;
    }
    private void NextFrameCallBack<T>(Action<string, T> callback,string key,T result)
    {
        StartCoroutine(NextFrameCallBackIe(callback,key,result));
    }

    private IEnumerator NextFrameCallBackIe<T>(Action<string, T> callback, string key, T result)
    {
        //callback(key, result);
        yield return 1;
        callback(key, result);
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


    public void ReleaseRefAsset(string key)
    {
        RefPool.ReleaseObject(key);
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

    public void ReleaseCallBack(string key, Action<string, UnityEngine.Object> callback)
    {
        for (int i = 0; i < loadQueueRequests.Count; i++)
        {
            var req = loadQueueRequests[i];
            if (req.callback.Equals(callback))
            {
                loadQueueRequests.RemoveAt(i);
                return;
            }
        }
        if (cancelRequests.TryGetValue(key,out List<LoadRequest> list))
        {
            list.Add(new LoadRequest(key,callback));
        }
        else
        {
            list = new List<LoadRequest>();
            list.Add(new LoadRequest(key, callback ));
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
                    list.RemoveAt(i);
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

public class LoadRequest
{
    public string key;
    public Action<string, UnityEngine.Object> callback;
    public LoadRequest(string key, Action<string, UnityEngine.Object> callback){
        this.key = key;
        this.callback = callback;

    }
}
