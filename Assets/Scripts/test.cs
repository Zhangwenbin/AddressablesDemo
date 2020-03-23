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
    public int count;
    public float size;

    public string status;
    public string checks;
    private void StartChcek()
    {
        status = "begin";
        Addressables.InitializeAsync().Completed+=initialCompleted;

            
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



    

    public string testKey;
    private void Update()
    {
     
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {

            Addressables.InstantiateAsync(testKey);
        }


        
    }


}
