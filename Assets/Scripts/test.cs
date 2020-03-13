using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Video;

public class test : MonoBehaviour
{
    public int count;
    public float size;

    public string status;
    public string checks;
    private void StartChcek()
    {
        status = "begin";
        Addressables.InitializeAsync().Completed+=initialCompleted;

        
        // Addressables.LoadAssetAsync<VideoClip>("movie").Completed+=onStart;
    }

    private void initialCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
        Addressables.CheckForCatalogUpdates(true).Completed += checkComplete;
       
    }

    private void checkComplete(AsyncOperationHandle<List<string>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status==AsyncOperationStatus.Failed)
        {
            return;
        }
        Debug.Log(obj.Result.Count);
        count = obj.Result.Count;
        status = "checkfinish";
        if (count==0)
        {         
            return;
        }
        checks = "";
        for (int i = 0; i < obj.Result.Count; i++)
        {
            checks += obj.Result[i];
            
        }
        Debug.Log(checks);
       
      
        Addressables.UpdateCatalogs(obj.Result).Completed += updatecomplete;
    }



    private void updatecomplete(AsyncOperationHandle<List<IResourceLocator>> obj)
    {
        Debug.Log(obj.Status);
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        status = "updatefinish";
        size = 0;
        List<string> keys = new List<string>();
        foreach (var item in Addressables.ResourceLocators)
        {
            Debug.Log(item.LocatorId);
            foreach (var key  in item.Keys)
            {
                Debug.Log(key);
              
            }
           
        }
       
    }

    private void checksizeComplete(AsyncOperationHandle<long> obj)
    {
        if (obj.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        size += obj.Result/1024;

    }

    private void OnGUI()
    {
        GUILayout.Label("count: " + count);
        GUILayout.Label("size: " + size + "kb");
        GUILayout.Label("status:" + status);

        GUILayout.Label("checks:" + checks);
    }

    private void Update()
    {
     
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Addressables.InstantiateAsync("Prefabs/Cube.prefab");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Addressables.InstantiateAsync("Prefabs/Horse.prefab");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Addressables.InstantiateAsync("Prefabs/Sphere.prefab");
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            Addressables.LoadSceneAsync("Scenes/scene1.unity");
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            Addressables.LoadSceneAsync("Scenes/scene0.unity");
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            StartChcek();
        }
    }

  
}
