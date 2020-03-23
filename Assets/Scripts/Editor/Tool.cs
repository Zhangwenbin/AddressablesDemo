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

    [MenuItem("Tool/ConfigAll")]
    static void ConfigAll()
    {
        Debug.Log("start configall");
        var config = LoadConfigs();

        int index = 0;
        while (index<Settings.groups.Count)
        {
            var g = Settings.groups[index];
            if (g.ReadOnly||g.IsDefaultGroup())
            {
                index++;
                continue;
            }
            Settings.RemoveGroup(g);
        }
        foreach (var group in config.groups)
        {
            CreateGroupAndEntry(group);
        }
    }

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

    [MenuItem("Tool/StartEditorMode")]
    static void StartEditorMode()
    {
        ConfigAll();
        SetPlayMode(0);

    }

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

    static void SetBuildScript(int index)
    {
        Settings.ActivePlayerDataBuilderIndex = index;
    }

    static void SetPlayMode(int index)
    {
        Settings.ActivePlayModeDataBuilderIndex = index;
    }

    static void CreateGroupAndEntry(GroupConfigItem item)
    {
        var folders = new string[] { item.path };
        var assets = AssetDatabase.FindAssets("", folders);

       
        Debug.Log("assets.Length " + assets.Length);
        var settings = Settings;
        var group = settings.FindGroup(item.name);
        if (group==null)
        {
            group= settings.CreateGroup(item.name, false, false, false,null );
            
        }
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema==null)
        {
            schema= group.AddSchema<BundledAssetGroupSchema>();
        }
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
       
        schema.BundleMode = (BundledAssetGroupSchema.BundlePackingMode)(item.packMode);
        ContentUpdateGroupSchema contentUpdateGroupSchema= group.GetSchema<ContentUpdateGroupSchema>();
        if (contentUpdateGroupSchema==null)
        {
            contentUpdateGroupSchema = group.AddSchema<ContentUpdateGroupSchema>();
        }
        contentUpdateGroupSchema.StaticContent = true;


        foreach (var asset in assets)
        {          
            var path = AssetDatabase.GUIDToAssetPath(asset);
            Debug.Log(path);
            var file = FormatAddress(path);
           var entry= settings.CreateOrMoveEntry(asset, group, false, false);
            entry.address = file;
        }              
    }

    static string FormatAddress(string path)
    {
        string header = "Assets/";
        return path.Substring(header.Length);

    }

    static void InitAddressableAssetSettings()
    {
        Settings.DisableCatalogUpdateOnStartup = false;
        Settings.BuildRemoteCatalog = true;
    }

    static void CleanBuild()
    {
        AddressableAssetSettings.CleanPlayerContent(null);
        BuildCache.PurgeCache(false);
        Directory.Delete(Settings.RemoteCatalogBuildPath.GetValue(Settings),true);
    }

    static void Build()
    {      
        AddressableAssetSettings.BuildPlayerContent();
    }

    [MenuItem("Tool/StartLocalService")]
    static void StartLocalService()
    {
        IHostingService localSv=null;
        foreach (var sv in Settings.HostingServicesManager.HostingServices)
        {
            if (sv.DescriptiveName== "localSv")
            {
                localSv = sv;
                break;
            }
        }
        if (localSv==null)
        {
            string hostingName = string.Format("{0} {1}", "localService", Settings.HostingServicesManager.NextInstanceId);
            localSv = Settings.HostingServicesManager.AddHostingService(Settings.HostingServicesManager.RegisteredServiceTypes[0], hostingName);
        }

        localSv.DescriptiveName = "localSv";
        localSv.StartHostingService();
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath,string.Format("http://{0}:{1}",Settings.HostingServicesManager.GlobalProfileVariables["PrivateIpAddress"],localSv.ProfileVariables["HostingServicePort"]));
    }

    [MenuItem("Tool/BuildAndStartServer")]
    static void BuildAndStartServer()
    {
        AutoBuildAll();
        SetPlayMode(2);
        StartLocalService();
    }

    [MenuItem("Tool/ContenUpdate")]
    static void ContentUpdate()
    {
        ConfigAll();
        PrepareContentUpdate();
      
    }


    static void PrepareContentUpdate()
    {
        var tempPath = AddressableAssetSettingsDefaultObject.Settings.ConfigFolder+"/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";
        var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);
        Debug.Log(tempPath);
        ContentUpdateScript.CreateContentUpdateGroup(Settings, modifiedEntries, ContentUpdateGroup);
        var buildOp = ContentUpdateScript.BuildContentUpdate(Settings, tempPath);
    }


    [MenuItem("Tool/BuildExe")]
    static void BuildExe()
    {
        AutoBuildAll();
      var report=  BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "BuildExe/a.exe", BuildTarget.StandaloneWindows,BuildOptions.None);
        var summary = report.summary;
        Debug.Log(summary.result);
    }

}
