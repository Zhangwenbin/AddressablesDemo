using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
public class AssetManager 
{
    public bool IsInitialize;

    public void Initialize()
    {
        Addressables.InitializeAsync().Completed+=(res)=> { IsInitialize = true; };
    }

    public void CheckForCatalogUpdates()
    {
        Addressables.CheckForCatalogUpdates();
    }

    public UnityEngine.Object LoadAsset(string key)
    {
        var op = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
        var go = op.WaitForCompletion();
        return go;
    }

    public void LoadAssetAsync(string key, Action<string, UnityEngine.Object> callback)
    {
        Addressables.LoadAssetAsync<UnityEngine.Object>(key).Completed += (res) => {

            callback(key, res.Result);
        };
    }

    public void LoadSceneAsync(string key, Action<string> callback, LoadSceneMode loadSceneMode=LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100 )
    {
        Addressables.LoadSceneAsync(key, loadSceneMode,activateOnLoad,priority).Completed += (res) => {

            callback(key);
        };
    }
}
