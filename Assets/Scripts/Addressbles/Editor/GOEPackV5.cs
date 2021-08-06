using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
//#define UNITY_EDITOR 1
#if UNITY_EDITOR
using UnityEditor;
namespace EG
{
    //打包//
    public class MUPackV5
    {
        public static string[] shaderinclude = new string[] {

        };
        public static string shadervariantsMat = ConstPath.Pack_ShadervariantsMat;
        public static string sceneExport = ConstPath.Pack_SceneExport;
        public static string sceneBasic = ConstPath.Pack_sceneBasic;

        //分包方式//
        public static Dictionary<string, string> msArrayPackType = new Dictionary<string, string>(){
            {"OneBunlde","单一Bundle"},

            {"Per File","逐文件"},
            {"Per Dir","逐目录"},
            {"Per Atlas","逐图集"},
            {"Scene","场景"},
            {"Scene Split","场景切割"},
            {"UIPrefab","UI Prefab"},
            {"AudioPrefab","声音Prefab"},
            {"LuaFile","Lua脚本"},
            {"ShaderLib","shader"},
        };
        #region extentions
        //扩展名//        
        public const string m_packExt = ".pack.txt";


        public const string m_streamAssetsDir = ConstPath.Pack_StreamAssetsDir;
        public const string m_streamAssetsDiriOS = ConstPath.Pack_StreamAssetsDiriOS;

        public const string m_ConfigDir = ConstPath.Pack_ConfigDir;

        #endregion

        List<MUPackBundleV5> mPackItems = new List<MUPackBundleV5>();

        [DisplayName("源目录")]
        public string SrcDir
        {
            get;
            set;
        }
        [JsonField(JsonFieldTypes.HasChildren)]
        public List<MUPackBundleV5> PackItems
        {
            get { return mPackItems; }
            set { mPackItems = value; }
        }

         public bool ConfigureAssetImporter(bool silent, out string error, out List<AssetBundleBuild> builds)
        {
            Dictionary<string, List<string>> fileMapping = new Dictionary<string, List<string>>();
            builds = null;
            foreach (var i in mPackItems)
            {
                fileMapping.Clear();
                //预处理结束后，获取文件列表
                List<string> files = i.BuildSrcFileList(SrcDir);

                foreach (var file in files)
                {
                    bool isGenerated = false;
                    string bundleName = null;
                    string generatedPath = null;
                    string[] ExtraAssetsInBundle = null;
                    AssetImporter importer = AssetImporter.GetAtPath(file);
                    if (importer == null)
                        continue;
                    if (i.PackType == "Per Atlas")
                    {
                        TextureImporter ti = importer as TextureImporter;
                        if (ti == null)
                        {
                            error = string.Format("文件:{0} 不是一个有效的图片", file);
                            if (!silent)
                                UnityEditor.EditorUtility.DisplayDialog("打包失败",
                                                            error,
                                                            "确定");
                            return false;
                        }
                        if (ti.textureType != TextureImporterType.Sprite)
                        {
 
                        }
                        bundleName = i.BundleName + "_" + ti.spritePackingTag;
                        
                    }
                    else if (i.PackType == "Per File")
                    {
                        if (!string.IsNullOrEmpty(i.BundleName))
                            bundleName = i.BundleName + "_" + Path.GetFileNameWithoutExtension(file);
                        else
                            bundleName = Path.GetFileNameWithoutExtension(file);
                    }
                    else if (i.PackType == "Scene")
                    {
                        UnityEngine.SceneManagement.Scene scene = new UnityEngine.SceneManagement.Scene();
                        {
                            string name = Path.GetFileName(file);
                            generatedPath = Path.Combine(sceneExport, name);
                        }
                        
                        isGenerated = true;
                        if (!string.IsNullOrEmpty(i.BundleName))
                            bundleName = i.BundleName + "_" + Path.GetFileNameWithoutExtension(file);
                        else
                            bundleName = Path.GetFileNameWithoutExtension(file);
                    }
                    else if (i.PackType == "ShaderLib") {
                        if (!string.IsNullOrEmpty(i.BundleName))
                            bundleName = i.BundleName + "_" + Path.GetFileNameWithoutExtension(file);
                        else
                            bundleName = Path.GetFileNameWithoutExtension(file);
                        ExtraAssetsInBundle = AssetDatabase.GetDependencies(file);

                        //用相对路径找到cginc文件，也添加进去
                        List<string> _list = new List<string>(ExtraAssetsInBundle);
                        foreach (string cginc in shaderinclude)
                        {
                            _list.Add(cginc);
                        }
                        if (Directory.Exists(shadervariantsMat))
                        {
                            DirectoryInfo direction = new DirectoryInfo(shadervariantsMat);
                            FileInfo[] mat_files = direction.GetFiles("*", SearchOption.AllDirectories);
                            for (int j = 0; j < mat_files.Length; j++)
                            {
                                if (mat_files[j].Name.EndsWith(".meta"))
                                {
                                    continue;
                                }
                                _list.Add(shadervariantsMat + mat_files[j].Name);
                            }
                        }
                        ExtraAssetsInBundle = _list.ToArray();
                    }
                    else
                        bundleName = i.BundleName;
                    bundleName = bundleName.Replace(" ","_").ToLower() + ".bundle";
                    List<string> nameSet;
                    if (!fileMapping.TryGetValue(bundleName, out nameSet))
                    {
                        nameSet = new List<string>();
                        //第一个是packlabel,不是资源
                        nameSet.Add(i.PackLabel);
                        fileMapping[bundleName] = nameSet;
                    }

                    if (isGenerated)
                        nameSet.Add(generatedPath);
                    else
                        nameSet.Add(importer.assetPath);

                    //目前对于打包shader的时候添加额外的资源到bundle中
                    if (ExtraAssetsInBundle != null) {
                        for (int j = 0; j < ExtraAssetsInBundle.Length; j++) {
                            if (!nameSet.Contains(ExtraAssetsInBundle[j])) {
                                nameSet.Add(ExtraAssetsInBundle[j]);
                            }
                        }
                    }

                }
                
                int idx = 0;

                builds = new List<AssetBundleBuild>();
                //清理不应该被打包进去的文件
                foreach (var item in fileMapping)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = item.Key;
                    //第一个用来存label
                    if (!string.IsNullOrEmpty(item.Value[0]))
                    {
                        build.assetBundleVariant = item.Value[0];
                    }

                    item.Value.RemoveAt(0);
                    build.assetNames = item.Value.ToArray();
                    builds.Add(build);
                    idx++;
                }

                for (int j = 0; j < builds.Count; j++)
                {
                    if (builds[j].assetNames.Length == 1)
                    {
                        var bundleName = i.BundleName;
                        if (string.IsNullOrEmpty(i.BundleName))
                            bundleName = "unknowGroup";
                        AddressbleTool.CreateGroupAndEntry(bundleName, builds[j],BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
                    }
                    else
                    {
                        AddressbleTool.CreateGroupAndEntry(builds[j].assetBundleName, builds[j], BundledAssetGroupSchema.BundlePackingMode.PackTogether);
                    }
                }
                                
            }

           
            error = null;
            return true;
        }


    }
}

#endif
