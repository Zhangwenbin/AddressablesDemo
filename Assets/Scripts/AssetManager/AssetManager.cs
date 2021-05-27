using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
public class AssetManager 
{

    public enum ReleaseType
    {
        OnNewScene,
        Manual,
        NoneRelease
    }
    public static AssetManager Instance = new AssetManager();
    public bool IsInitialize;

    public void Initialize()
    {
        Addressables.InitializeAsync().Completed+=(res)=> { IsInitialize = true; };
    }

    public void CheckForCatalogUpdates()
    {
        Addressables.CheckForCatalogUpdates();
    }

    /// <summary>
    /// 有可能卡死
    /// </summary>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    //public UnityEngine.Object LoadAsset(string key)
    //{
    //    var op = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
    //    var go = op.WaitForCompletion();
    //    Addressables.Release(op);
    //    return go;
    //}


    public void LoadAssetAsync(string key, Action< UnityEngine.Object, AsyncOperationHandle> callback)
    {
        var op = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
            op.Completed += (res) => {
            if (callback != null)
            {
                callback(res.Result,op);
            }
        };
    }

    public void LoadAssetAsync<T>(string key, Action<T, AsyncOperationHandle> callback)
    {
        var op = Addressables.LoadAssetAsync<T>(key);
        op.Completed += (res) => {
            if (callback != null)
            {
                callback(res.Result, op);
            }
        };
    }

    public void LoadGameObjectAsync(string key, Action<GameObject, AsyncOperationHandle> callback)
    {
        var op = Addressables.LoadAssetAsync<GameObject>(key);
        op.Completed += (res) => {
            if (callback != null)
            {
                callback(res.Result, op);
            }
        };
    }

    public void LoadSceneAsync(string key, Action<string> callback, LoadSceneMode loadSceneMode=LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100 )
    {
        Addressables.LoadSceneAsync(key, loadSceneMode,activateOnLoad,priority).Completed += (res) => {

            if (callback!=null)
            {
                callback(key);
            }
           
        };
    }
}
