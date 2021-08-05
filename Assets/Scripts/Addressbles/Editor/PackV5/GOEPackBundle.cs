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
    public class MUPackBundle
    {
        static public Func<string, string, bool, string[]> mGetFilesFunc = null;
        static public Func<string, long> mGetFileLengthFunc = null;

        private string mBundleName = "";
        private string mDepOn = "";
        private bool mbCollectDep = true;
        private string[] mSearchFilters = new string[] { };
        private bool mbSearchSubDir = true;
        private bool mDepByOther = false;
        private string[] mNecessaryFilters = new string[] { };
        private string[] mExcludeFilters = new string[] { };

        [DisplayName("Bundle名")]
        public string BundleName
        {
            set { mBundleName = value; }
            get { return mBundleName; }
        }
        [DisplayName("搜索Filter")]
        public string[] SearchFilters
        {
            set { mSearchFilters = value; }
            get { return mSearchFilters; }
        }
        [DisplayName("必要搜索Filter")]
        internal string NecessaryFiltersAsString
        {
            set { mNecessaryFilters = JsonUtil.StringToStringArray(value); }
            get { return JsonUtil.StringArrayToString(mNecessaryFilters); }
        }
        public string[] NecessaryFilters
        {
            set { mNecessaryFilters = value; }
            get { return mNecessaryFilters; }
        }
        [DisplayName("排除搜索Filter")]
        internal string ExcludeFiltersAsString
        {
            set { mExcludeFilters = JsonUtil.StringToStringArray(value); }
            get { return JsonUtil.StringArrayToString(mExcludeFilters); }
        }
        public string[] ExcludeFilters
        {
            set { mExcludeFilters = value; }
            get { return mExcludeFilters; }
        }
        [DisplayName("依赖于打包")]
        public string DependOnBundle
        {
            set { mDepOn = value; }
            get { return mDepOn; }
        }
        [DisplayName("是否搜索子目录")]
        public bool SearchSubDir
        {
            set { mbSearchSubDir = value; }
            get { return mbSearchSubDir; }
        }
        [DisplayName("是否收集依赖")]
        public bool CollectDep
        {
            set { mbCollectDep = value; }
            get { return mbCollectDep; }
        }
        [DisplayName("是否被依赖")]
        public bool DepByOther
        {
            set { mDepByOther = value; }
            get { return mDepByOther; }
        }

        public MUPackBundle()
        {
        }

        public void SetSearchFilter(string filters)
        {
            mSearchFilters = JsonUtil.StringToStringArray(filters);
        }

        public string GetSearchFilterStr()
        {
            return JsonUtil.StringArrayToString(mSearchFilters);
        }

        public UnityEditor.BuildAssetBundleOptions GetBuildOption()
        {
            UnityEditor.BuildAssetBundleOptions option = 0;//UnityEditor.BuildAssetBundleOptions.CollectDependencies;
            if (mbCollectDep)
                option |= UnityEditor.BuildAssetBundleOptions.CollectDependencies | UnityEditor.BuildAssetBundleOptions.CompleteAssets;
            option |= UnityEditor.BuildAssetBundleOptions.DeterministicAssetBundle;
            return option;
        }

        //是否满足必须串//
        bool IsMatchNecessary(string str)
        {
            if (null == mNecessaryFilters || mNecessaryFilters.Length == 0)
            {
                return true;
            }
            foreach (string filter in mNecessaryFilters)
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
            if (null == mExcludeFilters || mExcludeFilters.Length == 0)
            {
                return true;
            }
            foreach (string filter in mExcludeFilters)
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
            foreach (string filter in SearchFilters)
            {
                string name = Path.GetFileName(filter);
                if (name.StartsWith("*."))
                {
                    string path = Path.GetDirectoryName(filter);

                    string[] filenames = mGetFilesFunc(Path.Combine(srcPath, path), name, SearchSubDir);
                    foreach (string filename in filenames)
                    {
                        string str = filename.Replace('\\', '/');//UNITY要求必须为'/'//
                        if (IsMatchNecessary(str) && IsMatchExclude(str))
                            srcFiles.Add(str);
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
