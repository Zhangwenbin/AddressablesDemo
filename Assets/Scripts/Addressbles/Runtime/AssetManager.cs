using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace EG
{
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

        public Dictionary<AsyncOperationHandle, GameObject> handleDic =
            new Dictionary<AsyncOperationHandle, GameObject>();

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
            StartCoroutine(PreLoadAssetsIe(callback, showProgress));
        }

        private IEnumerator PreLoadAssetsIe(Action callback, bool showProgress = false)
        {
            if (showProgress)
            {
                LoadingScreen.SetTipsStatic(6);
            }

            var handle = Addressables.LoadAssetsAsync<Object>((IEnumerable) new List<string>() {".bytes", "lua"}, null,
                Addressables.MergeMode.Intersection);
            while (!handle.IsDone)
            {
                if (showProgress)
                {
                    LoadingScreen.SetProgress(handle.PercentComplete);
                }

                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var list = handle.Result as List<Object>;
                for (int i = 0; i < list.Count; i++)
                {
                    var txt = list[i] as TextAsset;
                    if (txt)
                    {
                        luaBytes[txt.name + ".bytes"] = txt.bytes;
                    }

                }
            }

            ReleaseHandle(handle, true);


            if (callback != null)
            {
                callback();
            }
        }

        private Dictionary<string, byte[]> luaBytes = new Dictionary<string, byte[]>();

        /// <summary>
        /// 注册不需要加载的资源
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public virtual void RegisterAsset(string key, GameObject obj)
        {
            if (obj != null)
            {
                GameObjectPool.RecycleGameObject(key, obj);
            }
        }

        public byte[] GetLuaBytes(string key)
        {
            if (luaBytes.ContainsKey(key))
            {
                return luaBytes[key];
            }

            return null;
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
                            ReleaseHandle(loadingReq.handle, true);
                            break;
                        case LoadRequest.AssetFrom.GameobjectPool:
                            GameObjectPool.RecycleGameObject(loadingReq.key, loadingReq.Result as GameObject);
                            break;
                        case LoadRequest.AssetFrom.RefPool:
                            RefPool.ReleaseObject(loadingReq.key);
                            break;
                        case LoadRequest.AssetFrom.LoadGameobjectInstantiate:
                            GameObjectPool.RecycleGameObject(loadingReq.key, loadingReq.Result as GameObject);
                            break;
                        case LoadRequest.AssetFrom.LoadGameobjectNotInstantiate:
                            ReleaseHandle(loadingReq.handle, true);
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
                    loadingReq.CallBack();
                    loadingRequests.Remove(loadingReq);
                    loadingCount = loadingRequests.Count;
                    continue;
                }

                break;
            }
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
                loadingRequests.Insert(0, req);
            }

            req.Load(this);
            return req;
        }


        public void LoadSceneAsync(string key, Action<string> callback,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
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
                if (req.key == key && req.callback.Equals(callback))
                {
                    comingRequests.Remove(req);
                    return;
                }
            }

            var cancelReq = new LoadRequest(key);
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
            GameobjectPool,
            RefPool,
            LoadGameobjectNotInstantiate,
            LoadGameobjectInstantiate,
            LoadRefAsset
        }

        private AssetManager rgr;

        public string key;
        public bool isSprite;
        public bool useCache;

        public AssetFrom aasetFrom;

        public AssetFrom AasetFrom
        {
            get { return aasetFrom; }
        }

        /// <summary>
        /// for load
        /// </summary>
        public AsyncOperationHandle handle;

        public void Load(AssetManager rgr)
        {
            this.rgr = rgr;
            aasetFrom = AssetFrom.None;

            var item = GameObjectPool.GetGameObject(key);
            if (item != null)
            {
                item.SetActive(true);
                this.useCache = true;
                this.result = item;
                aasetFrom = AssetFrom.GameobjectPool;
                return;
            }

            var refItem = RefPool.GetObject(key);
            if (refItem != null)
            {
                this.useCache = true;
                this.result = refItem;
                aasetFrom = AssetFrom.RefPool;
                return;
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
        public bool IsDone
        {
            get { return useCache || handle.IsDone; }
        }


        /// <summary>
        /// Check if the internal operation is not null and has the same version of this handle.
        /// </summary>
        /// <returns>True if valid.</returns>
        public bool IsValid
        {
            get { return useCache || handle.IsValid(); }
        }


        /// <summary>
        /// The progress of the internal operation.
        /// This is evenly weighted between all sub-operations. For example, a LoadAssetAsync call could potentially
        /// be chained with InitializeAsync and have multiple dependent operations that download and load content.
        /// In that scenario, PercentComplete would reflect how far the overal operation was, and would not accurately
        /// represent just percent downloaded or percent loaded into memory.
        /// For accurate download percentages, use GetDownloadStatus(). 
        /// </summary>
        public float PercentComplete
        {
            get { return useCache ? 1 : handle.PercentComplete; }
        }

        private Object result;

        /// <summary>
        /// The result object of the operations.
        /// </summary>
        public Object Result
        {
            get
            {
                if (IsDone)
                {
                    if (Status == AsyncOperationStatus.Succeeded)
                    {
                        if (result == null)
                        {
                            GameObject go = handle.Result as GameObject;
                            if (go)
                            {
                                // if (EngineDelegate.PrefabUnInstantiateRule != null &&
                                //     EngineDelegate.PrefabUnInstantiateRule(go))
                                // {
                                //     result = go;
                                //     aasetFrom =LoadRequest.AssetFrom.LoadGameobjectNotInstantiate;
                                // }
                                // else
                                {
                                    var instance = UnityEngine.GameObject.Instantiate(go);
                                    result = instance;
                                    rgr.handleDic[handle] = instance;
                                    aasetFrom = LoadRequest.AssetFrom.LoadGameobjectInstantiate;
                                }

                            }
                            else
                            {
                                aasetFrom = LoadRequest.AssetFrom.LoadRefAsset;
                                RefPool.OnLoadObject(key, handle);
                                result = handle.Result as Object;
                            }


                        }
                    }
                    else
                    {
                        Debug.LogError("加载资源失败 :" + key);
                    }
                }

                return result;
            }
        }


        /// <summary>
        /// The status of the internal operation.
        /// </summary>
        public AsyncOperationStatus Status
        {
            get { return useCache ? AsyncOperationStatus.Succeeded : handle.Status; }
        }


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
}