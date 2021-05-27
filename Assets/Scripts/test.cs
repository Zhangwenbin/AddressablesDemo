using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Video;

public class test : MonoBehaviour
{
    private static int index;
    private void Start()
    {
        
        Addressables.InitializeAsync().Completed += initialCompleted;
    }

    private void initialCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {

        Addressables.CheckForCatalogUpdates(true).Completed += checkComplete;
    }

    private void checkComplete(AsyncOperationHandle<List<string>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        Debug.Log(obj.Result.Count);
        if (obj.Result.Count == 0)
        {
            return;
        }
        long size = 0;
        //for (int i = 0; i < obj.Result.Count; i++)
        //{
        //    Addressables.GetDownloadSizeAsync(obj.Result[i]).Completed+=(res)=> {
        //        size += res.Result;
        //    };
        //}
        Debug.Log(size);
        Addressables.UpdateCatalogs(obj.Result).Completed += updatecomplete;
    }



    private void updatecomplete(AsyncOperationHandle<List<IResourceLocator>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        for (int i = 0; i < obj.Result.Count; i++)
        {
            Debug.Log(obj.Result[i]);
        }
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            index++;
            index %= 2;
            AssetManager.Instance.LoadSceneAsync(string.Format("Scenes/scene{0}.unity",index),null);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (handles.Count>1)
            {
                Addressables.Release(handles[handles.Count-1]);
                handles.RemoveAt(handles.Count - 1);
            }

        }

    }

    public string testKey;
    public void TestSync()
    {
      //Instantiate( AssetManager.Instance.LoadAsset(testKey));
    }
    private static List<AsyncOperationHandle> handles=new List<AsyncOperationHandle>();
    public void TestASync()
    {
        //AssetManager.Instance.LoadAssetAsync(testKey, (res,op) =>
        //{
        //    Instantiate(res);
        //    handles.Add(op);
        //});
        AssetManager.Instance.LoadAssetAsync<TextAsset>(testKey, (res, op) =>
        {
            Debug.Log(res.text);
            handles.Add(op);
        });
    }



}
