using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Build;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using System.Linq;
using UnityEngine.SocialPlatforms;

public class Tool 
{
    [Serializable]
    public class GroupConfigItem
    {
        public string path;
        public int packMode;
        public string name;
        public string locate;
    }

    [Serializable]
    public class GroupsConfig
    {
        public GroupConfigItem[] groups;
    }

    static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings Settings
    {
        get { return UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true); }
    }

    static readonly string ContentUpdateGroup = "contentUpdate";



    /// <summary>
    /// 编辑器模式
    /// </summary>
    [MenuItem("Tool/StartEditorMode")]
    static void StartEditorMode()
    {
        ConfigAll();
        SetPlayMode(0);

    }

    /// <summary>
    /// 配置bundle group
    /// </summary>
    [MenuItem("Tool/ConfigAll")]
    static void ConfigAll()
    {
        Debug.Log("start configall");
        //var config = LoadConfigs();

        //int index = 0;
        //while (index<Settings.groups.Count)
        //{
        //    var g = Settings.groups[index];
        //    if (g.ReadOnly||g.IsDefaultGroup())
        //    {
        //        index++;
        //        continue;
        //    }
        //    Settings.RemoveGroup(g);
        //}
        //foreach (var group in config.groups)
        //{
        //    CreateGroupAndEntry(group);
        //}
    }

    /// <summary>
    /// 自动build 资源
    /// </summary>
    [MenuItem("Tool/AutoBuildAll")]
   static void AutoBuildAll()
    {
        ConfigAll();
        SetBuildScript(3);
        SetPlayMode(2);
        InitAddressableAssetSettings();
        CleanBuild();
        Build();
       
    }


    [MenuItem("Tool/StartLocalService")]
    static void StartLocalService()
    {
        IHostingService localSv = null;
        foreach (var sv in Settings.HostingServicesManager.HostingServices)
        {
            if (sv.DescriptiveName == "localSv")
            {
                localSv = sv;
                break;
            }
        }
        if (localSv == null)
        {
            string hostingName = string.Format("{0} {1}", "localService", Settings.HostingServicesManager.NextInstanceId);
            localSv = Settings.HostingServicesManager.AddHostingService(Settings.HostingServicesManager.RegisteredServiceTypes[0], hostingName);
        }

        localSv.DescriptiveName = "localSv";
        localSv.StartHostingService();
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, string.Format("http://{0}:{1}", Settings.HostingServicesManager.GlobalProfileVariables["PrivateIpAddress"], localSv.ProfileVariables["HostingServicePort"]));
    }

    [MenuItem("Tool/BuildAndStartServer")]
    static void BuildAndStartServer()
    {
        AutoBuildAll();
        SetPlayMode(2);
        StartLocalService();
    }



    [MenuItem("Tool/BuildExe")]
    static void BuildExe()
    {
        AutoBuildAll();
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "BuildExe/a.exe", BuildTarget.StandaloneWindows, BuildOptions.None | BuildOptions.Development | BuildOptions.AllowDebugging);
        var summary = report.summary;
        Debug.Log(summary.result);
    }

    [MenuItem("Tool/ContenUpdate")]
    static void ContentUpdate()
    {
        ConfigAll();
        PrepareContentUpdate();

    }
    static void PrepareContentUpdate()
    {
        var tempPath = AddressableAssetSettingsDefaultObject.Settings.ConfigFolder + "/" + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
        var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);
        Debug.Log(tempPath);
        ContentUpdateScript.CreateContentUpdateGroup(Settings, modifiedEntries, ContentUpdateGroup);
        var buildOp = ContentUpdateScript.BuildContentUpdate(Settings, tempPath);
    }


    /// <summary>
    /// 加载配置
    /// </summary>
    /// <returns></returns>
    static GroupsConfig LoadConfigs()
    {
        var config = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/bundleconfig.json").text;
        GroupsConfig g = JsonUtility.FromJson<GroupsConfig>(config);
        Debug.Log("groups count:"+ g.groups.Length);
        return g;
    }

    static void CreateProfile(string name,string copyName)
    {
        Settings.activeProfileId = Settings.profileSettings.AddProfile(name,Settings.profileSettings.GetProfileId(copyName));         
    }

    /// <summary>
    /// 设置build脚本
    /// </summary>
    /// <param name="index"></param>
    static void SetBuildScript(int index)
    {
        Settings.ActivePlayerDataBuilderIndex = index;
    }


    /// <summary>
    /// 设置运行模式
    /// </summary>
    /// <param name="index"></param>
    static void SetPlayMode(int index)
    {
        Settings.ActivePlayModeDataBuilderIndex = index;
    }

    /// <summary>
    /// 设置资源分组示例
    /// </summary>
    /// <param name="item"></param>
    static void CreateGroupAndEntry(GroupConfigItem item)
    {
        var folders = new string[] { item.path };
        var assets = AssetDatabase.FindAssets("", folders);

       
        Debug.Log("assets.Length " + assets.Length);
        var settings = Settings;
        var group = settings.FindGroup(item.name);
        if (group==null)
        {
            group= settings.CreateGroup(item.name, false, false, false,null );//创建分组group
            
        }
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema==null)
        {
            schema= group.AddSchema<BundledAssetGroupSchema>();//创建加载策略组件
        }

        //设置build和load路径local放在本地streaming文件夹  remote放在服务器   一般都设置为local
        if (item.locate=="host")
        {
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        }
        else
        {
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        }
       
        //设置bundlemode 打包在一起还是分开
        schema.BundleMode = (BundledAssetGroupSchema.BundlePackingMode)(item.packMode);

        //设置更新策略 
        //local static 随包一起, build进入streaming文件夹内, 更新后, 进入contentupdate分组, 放到服务器上, 准备下载
        //local dynamic 随包一起,build进入streaming文件夹内,很奇怪,不能更新
        //remote static 不进入包体, build后在serverdata目录内, 需要放到服务器上, 更新后, 进入contentupdate分组, 准备下载
        //remote dynamic 不进入包体,build后在serverdata目录内,需要放到服务器上,更新后还在原来的分组内,准备下载
        ContentUpdateGroupSchema contentUpdateGroupSchema= group.GetSchema<ContentUpdateGroupSchema>();
        if (contentUpdateGroupSchema==null)
        {
            contentUpdateGroupSchema = group.AddSchema<ContentUpdateGroupSchema>();
        }
        contentUpdateGroupSchema.StaticContent = true;

        //创建资源地址
        foreach (var asset in assets)
        {          
            var path = AssetDatabase.GUIDToAssetPath(asset);
            Debug.Log(path);
            var file = FormatAddress(path);
           var entry= settings.CreateOrMoveEntry(asset, group, false, false);
            entry.address = file;
        }              
    }

    /// <summary>
    /// 资源命名规则
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static string FormatAddress(string path)
    {
        string header = "Assets/";
        return path.Substring(header.Length);

    }

    /// <summary>
    /// 初始化设置  禁用自动更新,构建服务器目录
    /// </summary>
    static void InitAddressableAssetSettings()
    {
        Settings.DisableCatalogUpdateOnStartup = false;
        Settings.BuildRemoteCatalog = true;
    }


    /// <summary>
    /// 清理构建内容
    /// </summary>
    static void CleanBuild()
    {
        AddressableAssetSettings.CleanPlayerContent(null);
        BuildCache.PurgeCache(false);
        string cachePath = Settings.RemoteCatalogBuildPath.GetValue(Settings);
        if (Directory.Exists(cachePath)) {
            Directory.Delete(Settings.RemoteCatalogBuildPath.GetValue(Settings), true);
        }
       
    }

    /// <summary>
    /// 构建addresble资源
    /// </summary>
    static void Build()
    {      
        AddressableAssetSettings.BuildPlayerContent();
    }


}
