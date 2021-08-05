using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;


namespace EG
{
#if UNITY_EDITOR
    //打包项//
    public class MUPackBundleV5
    {
        string nFilter, eFilter;
        string[] nFilterArr, eFilterArr;
        [DisplayName("Bundle名")]
        public string BundleName
        {
            get;
            set;
        }

        [DisplayName("子目录")]
        public string SubFolder
        {
            get;
            set;
        }
        [DisplayName("搜索Filter")]
        public string[] SearchFilters
        {
            get;
            set;
        }
        [DisplayName("必要搜索Filter")]
        public string NecessaryFilters
        {
            get { return nFilter; }
            set
            {
                if (nFilter != value)
                {
                    nFilter = value;
                    RebuildNecessaryArray();
                }
            }
        }
        [DisplayName("排除搜索Filter")]
        public string ExcludeFilters
        {
            get { return eFilter; }
            set
            {
                if (eFilter != value)
                {
                    eFilter = value;
                    RebuildExcludeArray();
                }
            }
        }
        [DisplayName("是否搜索子目录")]
        public bool SearchSubDir
        {
            get;
            set;
        }

        [DisplayName("是否收集依赖")]
        public bool CollectDepends
        {
            get;
            set;
        }

        [JsonFieldAttribute(JsonFieldTypes.PackType)]
        [DisplayName("打包类型")]
        public string PackType
        {
            get;
            set;
        }
        public MUPackBundleV5()
        {
        }

        void RebuildNecessaryArray()
        {
            nFilterArr = nFilter.Split(',');
        }

        void RebuildExcludeArray()
        {
            eFilterArr = eFilter.Split(',');
        }
        //是否满足必须串//
        bool IsMatchNecessary(string str)
        {
            if (string.IsNullOrEmpty(NecessaryFilters))
            {
                return true;
            }
            if (nFilterArr == null)
                RebuildNecessaryArray();
            foreach (string filter in nFilterArr)
            {
                if (!str.Contains(filter))
                {
                    return false;
                }
            }
            return true;
        }
        //是否满足排除串//
        bool IsMatchExclude(string str)
        {
            if (string.IsNullOrEmpty(ExcludeFilters))
            {
                return true;
            }
            if (eFilterArr == null)
                RebuildExcludeArray();
            foreach (string filter in eFilterArr)
            {
                if (str.Contains(filter))
                {
                    return false;
                }
            }
            return true;
        }

        //构造源文件列表//
        public List<string> BuildSrcFileList(string srcPath)
        {
            List<string> srcFiles = new List<string>();
            if (SubFolder == null)
                SubFolder = "";
            foreach (string filter in SearchFilters)
            {
                string name = Path.GetFileName(filter);
                if (name.StartsWith("*."))
                {
                    string path = Path.GetDirectoryName(filter);
                    SearchOption option = SearchSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    string dirPath = Path.Combine(Path.Combine(srcPath, SubFolder), path);
                    if (!Directory.Exists(dirPath))
                        continue;

                    DirectoryInfo info = new DirectoryInfo( dirPath );
                    if ( !info.Exists )
                    {
                        continue;
                    }
                    //校正一下目录，去掉 ../, 把相对目录转成绝对路径
                    dirPath = info.FullName.Substring( info.FullName.IndexOf( "Assets" ) );
                    string[] filenames = Directory.GetFiles(dirPath, name, option);
                    foreach (string filename in filenames)
                    {
                        string str = filename.Replace('\\', '/');//UNITY要求必须为'/'//
                        if (IsMatchNecessary(str) && IsMatchExclude(str))
                        {
                            if (BundleName == "ui_img_font" && (filename.Contains("Fighting.png") || filename.Contains("JiYuan.png")))
                            {
                                continue;
                            }
                            else
                            {
                                srcFiles.Add(str);
                            }
    
                        }
                            
                    }
                }
                else
                {
                    //无通配符的情况//
                    string filename = Path.Combine(srcPath, filter);
                    srcFiles.Add(filename.Replace('\\', '/'));//UNITY要求必须为'/'//
                }
            }
            return srcFiles; 
        }
    }
#endif

}
