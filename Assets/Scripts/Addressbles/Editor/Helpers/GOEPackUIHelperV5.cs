using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
//using libxl;
using System;


namespace EG
{
    //打包UIHelper//
    public class MUPackUIHelperV5 : JsonObjectUIHelper
    {
        bool dirty = false;
        static string osPath = ConstPath.Pack_LuaGeneratePath;
        static string outDir = ConstPath.Pack_LuaOutPath;
        static string luaPath = ConstPath.Pack_LuaSrcPath;

        MUPackV5 curPack;

        public MUPackUIHelperV5()
        {

        }

        //是否需要先启动游戏//
        public override bool DoesNeedStarted() { return false; }
        //搜索目录还是文件//
        public override bool SearchForDir() { return false; }
        //搜索目录//
        public override string GetSearchDir() { return ConstPath.Pack_V5HelperSearchDir; }
        //扩展名//
        public override string GetFileExt() { return ".pack.txt"; }

        public override bool CanMultiple
        {
            get
            {
                return true;
            }
        }

        public override string MultipleActionName
        {
            get
            {
                return "打包";
            }
        }

        public override bool IsDirty
        {
            get
            {
                return dirty;
            }
        }

        //new//
        public override bool CanNew() { return true; }
        public override object OnNew()
        {
            return new MUPackV5();
        }
        //save//
        public override bool CanSave() { return true; }
        //delete//
        public override bool CanDelete() { return true; }
        //select//
        public override object OnSelect(string strFullName)
        {
            //清空当前数据//
            curPack = null;
            //mDep.Reset();

            //读文件//
            MUPackV5 si = JsonUtil.ReadJsonObject(strFullName) as MUPackV5;
            if (null != si)
            {
                curPack = si;
            }
            else
            {
                Debug.Log("parse error:");
            }

            return curPack;
        }

        public override Dictionary<string, string> EnumOptions(object target, string paramName)
        {
            if (target.GetType() == typeof(MUPackBundleV5))
            {
                //打包//
                MUPackBundleV5 pack = target as MUPackBundleV5;
                JsonFieldTypes pc = JsonFieldAttribute.GetFieldFlag(target, paramName);
                if (null != pc)
                {
                    if (pc == JsonFieldTypes.PackType)
                    {
                        //分包方式//
                        return MUPackV5.msArrayPackType;
                    }
                }
            }
            return null;
        }


 
        public override void MakeEditUI(object target)
        {
            MUPackV5 pack = target as MUPackV5;
            if (null == pack)
                return;

            GUILayout.BeginVertical();


            //打包项编辑//
            GUILayout.Label("编辑Bundles");
            for (int i = 0; i < pack.PackItems.Count; i++)
            {
                MUPackBundleV5 bundle = pack.PackItems[i];
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {//两个宽度30的按钮＝一个宽度64的按钮//
                    //删除打包项//
                    pack.PackItems.RemoveAt(i);
                }
                GUILayout.Label(bundle.BundleName);

           
                GUILayout.EndHorizontal();
            }
            //添加//
            if (GUILayout.Button("添加Bundle", GUILayout.Width(140)))
            {
                pack.PackItems.Add(new MUPackBundleV5());
                //更新属性窗口//
                JsonObjectPropertyWindow wp = EditorWindow.GetWindow(typeof(JsonObjectPropertyWindow), false, "Property") as JsonObjectPropertyWindow;
                wp.Show();
                wp.SetCurStringInterface(pack, this);
            }

            GUILayout.EndVertical();
        }

      

        public static void copyLuaFile()
        {
#if UNITY_IOS
            CopyLuaBytesFiles(luaPath,osPath,false);
#elif UNITY_ANDROID
            // mac上打android 启用jit
            if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                CopyLuaBytesFiles(luaPath, osPath, true);
            else
                CopyLuaBytesFiles(luaPath, osPath, false);
#else
            CopyLuaBytesFiles(luaPath, osPath, false);
#endif

        }


        static void CopyLuaBytesFiles(string sourceDir, string destDir, bool bjit = false)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);
            Directory.CreateDirectory(destDir);

            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            int len = sourceDir.Length;
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                string ext = Path.GetExtension(files[i]);

                if (ext != ".lua" && ext != ".bytes")
                    continue;
                string dest = destDir + name;
                string dir = Path.GetDirectoryName(dest);
                Directory.CreateDirectory(dir);
                if (File.Exists(dest))
                {
                    Debug.LogError(dest + "已经存在!");
                }
                File.Copy(files[i], dest, true);
            }
            if (bjit)
                getLuajit();
            else
                getLua();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void getLua()
        {

            string[] files = Directory.GetFiles(osPath, "*.*", SearchOption.AllDirectories);
            int len = files.Length;
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                string ext = Path.GetExtension(files[i]);

                if (ext != ".lua" && ext != ".bytes")
                    continue;

                string dest = outDir + name + ".bytes";
                string dir = Path.GetDirectoryName(dest);
                Directory.CreateDirectory(dir);
                File.Copy(files[i], dest, true);
            }
        }

        private static void getLuajit()
        {
            System.Diagnostics.Process luaJitProcess = new System.Diagnostics.Process();
            if (System.Environment.OSVersion.Platform == PlatformID.MacOSX || System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                luaJitProcess.StartInfo.UseShellExecute = false;
                luaJitProcess.StartInfo.FileName = "/bin/sh";
#if UNITY_ANDROID
                string path = Application.dataPath + "/Lua/jitAndroid/Build.sh";
#else
                 string path = Application.dataPath + "/Lua/Build64.sh";
#endif
                luaJitProcess.StartInfo.Arguments = path;
            }
            else
            {
#if UNITY_ANDROID
                string path = Application.dataPath + "/Lua/jitAndroid/Build.bat";
#else
                string path = Application.dataPath + "/Lua/Build64.bat";
#endif
                luaJitProcess.StartInfo.FileName = path;
                luaJitProcess.StartInfo.CreateNoWindow = true;
            }

            luaJitProcess.Start();
            luaJitProcess.WaitForExit();
            luaJitProcess.Close();
        }


    }



}