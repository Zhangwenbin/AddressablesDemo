using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Video;
using UnityEngine.UI;
public class AddressableTest : MonoBehaviour
{
    public Text statusText, updateCountText, updateSizeText, lsText, rsText, rdText;

    public string lsKey, rsKey, rdKey;

    private void Start()
    {
        statusText.text = "InitializeAsync";
        Addressables.InitializeAsync().Completed += initialCompleted;
    }
    
    private void initialCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
        statusText.text = "CheckForCatalogUpdates";
        Addressables.CheckForCatalogUpdates(true).Completed += checkComplete;
    }

    private void checkComplete(AsyncOperationHandle<List<string>> obj)
    {
        statusText.text = "checkComplete";
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        updateCountText.text = obj.Result.Count+"";
        Debug.Log(obj.Result.Count);
        if (obj.Result.Count == 0)
        {
            DownLoadSize();
            return;
        }

        Addressables.UpdateCatalogs(obj.Result).Completed += updatecomplete;
    }



    private void updatecomplete(AsyncOperationHandle<List<IResourceLocator>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        DownLoadSize();
    }

    public void TestASync()
    {
        AssetManager.Instance.LoadAssetAsync<TextAsset>(lsKey, (res, op) =>
        {
            Debug.Log(res.text);
            lsText.text = res.text;
        });

        AssetManager.Instance.LoadAssetAsync<TextAsset>(rsKey, (res, op) =>
        {
            Debug.Log(res.text);
            rsText.text = res.text;
        });
        AssetManager.Instance.LoadAssetAsync<TextAsset>(rdKey, (res, op) =>
        {
            Debug.Log(res.text);
            rdText.text = res.text;
        });
    }

    public void DownLoadAll()
    {
        var locators = Addressables.ResourceLocators;
        foreach (var item in locators)
        {
            var keys = item.Keys;
            foreach (var key in keys)
            {
                Addressables.DownloadDependenciesAsync(key).Completed += (res) => {
                   
                };
            }
        }
    }

    public void DownLoadSize()
    {
        long size = 0;
        var locators = Addressables.ResourceLocators;
        foreach (var item in locators)
        {
            var keys = item.Keys;
            foreach (var key in keys)
            {
                Addressables.GetDownloadSizeAsync(key).Completed += (res) => {
                    size += res.Result;
                    updateSizeText.text = size + "";
                };
            }
        }

        Debug.Log(size);

    }


}
