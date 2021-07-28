using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MUEngine
{
    public class AdressbleResource 
    {
        AssetManager _resMgr;
        AssetManager resMgr { get {
                if (_resMgr == null)
                {
                    var go = new GameObject("AssetManager");
                    _resMgr = go.AddComponent<AssetManager>();
                }
                return _resMgr;
            } }
        public  void Start() 
        {         
            resMgr.Initialize();
          
        }
        public  bool Restart()
        {
            bool succeed = true;
            
            return succeed;
        }
        public virtual void Shutdown()
        {

        }

        public bool IsReady
        {
           get{ return resMgr.IsInitialize ; }
        } 


        /// <summary>
        /// 从assetbundle中加载asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <param name="priority"></param>
        public virtual void GetAsset(string name, Action<string, UnityEngine.Object> callback, LoadPriority priority = LoadPriority.Default, ECacheType type = ECacheType.AutoDestroy)
        {
            // resourceMgr.AssetBundleGroup.GetAsset(name, callback, priority, type);
            if (string.IsNullOrEmpty(name))
            {
                callback(name, null);
                return ;
            }
            if (priority==LoadPriority.Default)
            {
                resMgr.LoadAssetAsyncQueue(name, callback);
            }
            else
            {
                resMgr.LoadAssetAsync<UnityEngine.Object>(name, callback);
            }
        
        }

        public void LoadSpriteAsync(string name, Action<string,UnityEngine.Sprite> callback)
        {
             resMgr.LoadAssetAsync<UnityEngine.Sprite>(name, (key,res)=> { callback(key, res as Sprite); });
        }

 


        /// <summary>
        /// 只能注册不需要加载的 已有的gameobject
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        public virtual void RegisterAsset(string name, GameObject obj)
        {
            resMgr.RegisterAsset(name, obj);  
        }

        //���س���
        public virtual void GetScene(string name, Action callBack = null)
        {
            resMgr.LoadSceneAsync(name+".unity", (res) => {
                if (callBack!=null)
                {
                    callBack();
                }            
            });         
        }


        /// <summary>
        /// 从本地加载二进制文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        public virtual void GetBytes(string name, Action<string, byte[]> callback, bool fromStream = false)
        {
             resMgr.LoadAssetAsync<TextAsset>(name,
              (key, res) => {
                  var txt = res as TextAsset;
                  callback(name, txt.bytes);
              }
              );
        }

        public virtual byte[] GetBytesSync(string name)
        {
           var op= resMgr.LoadAssetSync<TextAsset>(name);
            return op.bytes;
        }

        /// <summary>
        /// 从assetbundle中读取字符串
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public virtual void GetString(string name, Action<string, string> callback)
        {
             resMgr.LoadAssetAsync<TextAsset>(name,
               (key, res) => {
                   var txt = res as TextAsset;
                   callback(name, txt.text);
               }
               );
        }


        //ɾ����Դ����
        public virtual void ReleaseAsset(string name, UnityEngine.Object obj)
        {
            if (obj is GameObject)
            {
                GameObjectPool.RecycleGameObject(name, obj as GameObject);
            }
            else
            {
                ReleaseRefAsset(name);
            }
        }

        public void ReleaseRefAsset(string key)
        {
            resMgr.ReleaseRefAsset(key);
        }


        public void SetLowAsyncLoadPriority(bool value)
        {         
            if (value)
            {
                Application.backgroundLoadingPriority = ThreadPriority.Low;
            }
            else
            {
                Application.backgroundLoadingPriority = ThreadPriority.High;
            }
        }
   


        public string GetLocalVersionStr()
        {
            return "1.0.0";
        }

        public void ReleaseCallBack(string key, Action<string, UnityEngine.Object> callback)
        {
            resMgr.ReleaseCallBack(key,callback);
        }

        public void PreLoadAssets(Action callback, bool showProgress = false)
        {
            resMgr.PreLoadAssets(callback,showProgress);
        }
    }
}
